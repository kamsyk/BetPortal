import { Component, inject, OnInit, PLATFORM_ID, NO_ERRORS_SCHEMA } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { AllCommunityModule, ColDef, GridApi, IDatasource, IGetRowsParams, ModuleRegistry, themeAlpine } from 'ag-grid-community';
import { KsAgGridModule, KsAgGridServerRequest } from 'ks-ag-controls';
import { Sport, SportListQuery, SportService } from '../../../services/sport.service';
import { ThemeService } from '../../../services/theme.service';
import { GridSettingsService, GridSettingsData } from '../../../services/grid-settings.service';
import { debounceTime, Subject } from 'rxjs';

ModuleRegistry.registerModules([AllCommunityModule]);

@Component({
  selector: 'app-sports-list',
  standalone: true,
  imports: [CommonModule, KsAgGridModule],
  schemas: [NO_ERRORS_SCHEMA],
  templateUrl: './sports-list.component.html',
  styleUrl: './sports-list.component.scss'
})
export class SportsListComponent implements OnInit {
  private readonly sportService = inject(SportService);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly gridSettingsService = inject(GridSettingsService);
  readonly themeService = inject(ThemeService);

  private gridApi?: GridApi;
  private gridInitialized = false;
  private readonly pageIdentifier = 'sports-list';
  private readonly settingsChange$ = new Subject<void>();
  initialPageSize = 25;

  error = '';
  readonly isBrowser = isPlatformBrowser(this.platformId);
  readonly modules = [AllCommunityModule];
  readonly theme = themeAlpine;

  readonly columnDefs: ColDef<Sport>[] = [
    { field: 'name', headerName: 'Sport', flex: 20, minWidth: 160, sortable: true, filter: true },
    {
      field: 'active',
      headerName: 'Active',
      width: 110,
      sortable: true,
      filter: true,
      valueFormatter: (p) => (p.value ? 'Yes' : 'No'),
    },
  ];

  readonly defaultColDef: ColDef = {
    resizable: true,
  };

  serverDataSource: IDatasource = this.createServerDataSource();

  ngOnInit(): void {
    if (!this.isBrowser) {
      return;
    }

    // Load saved settings
    this.loadGridSettings();

    // Setup debounced settings save
    this.settingsChange$.pipe(debounceTime(1000)).subscribe(() => {
      this.saveGridSettings();
    });
  }

  private loadGridSettings(): void {
    this.gridSettingsService.getSettings(this.pageIdentifier).subscribe({
      next: (response) => {
        if (response.settingsJson) {
          const settings: GridSettingsData = JSON.parse(response.settingsJson);
          if (settings.pageSize) {
            this.initialPageSize = settings.pageSize;
          }
        }
      },
      error: () => {
        // Settings not found or error loading - use defaults
      },
    });
  }

  private applyGridSettings(): void {
    if (!this.gridApi) {
      return;
    }

    const cachedSettings = this.gridSettingsService.getCachedSettings(this.pageIdentifier);
    if (!cachedSettings) {
      return;
    }

    // Apply column state (includes visibility, order, width, etc.)
    if (cachedSettings.columnState && cachedSettings.columnState.length > 0) {
      this.gridApi.applyColumnState({ state: cachedSettings.columnState, applyOrder: true });
    }

    // Apply filter model
    if (cachedSettings.filterModel) {
      this.gridApi.setFilterModel(cachedSettings.filterModel);
    }
  }

  private saveGridSettings(): void {
    if (!this.gridApi) {
      return;
    }

    const settings: GridSettingsData = {
      pageSize: this.gridApi.paginationGetPageSize(),
      filterModel: this.gridApi.getFilterModel(),
      columnState: this.gridApi.getColumnState(),
    };

    this.gridSettingsService.saveSettings(this.pageIdentifier, settings).subscribe({
      error: () => {
        console.error('Failed to save grid settings');
      },
    });
  }

  private createServerDataSource(): IDatasource {
    return {
      getRows: (params: IGetRowsParams) => {
        // Initialize grid API on first call
        if (!this.gridInitialized && (params as any).api) {
          this.gridApi = (params as any).api;
          this.initializeGridSettings();
          this.gridInitialized = true;
        }

        const request: KsAgGridServerRequest = {
          startRow: params.startRow,
          endRow: params.endRow,
          sortModel: params.sortModel,
          filterModel: params.filterModel,
        };

        const query = this.buildQueryFromRequest(request);

        this.sportService.getSports(query).subscribe({
          next: (sports) => {
            params.successCallback(sports.slice(request.startRow, request.endRow), sports.length);
          },
          error: () => {
            this.error = 'Failed to load sports.';
            params.failCallback();
          },
        });
      },
    };
  }

  private initializeGridSettings(): void {
    if (!this.gridApi) {
      return;
    }

    this.applyGridSettings();

    // Listen to state changes
    this.gridApi.addEventListener('columnResized', () => this.settingsChange$.next());
    this.gridApi.addEventListener('columnMoved', () => this.settingsChange$.next());
    this.gridApi.addEventListener('columnVisible', () => this.settingsChange$.next());
    this.gridApi.addEventListener('sortChanged', () => this.settingsChange$.next());
    this.gridApi.addEventListener('filterChanged', () => this.settingsChange$.next());
    this.gridApi.addEventListener('paginationChanged', () => this.settingsChange$.next());
  }

  private refreshServerDataSource(): void {
    this.serverDataSource = this.createServerDataSource();
  }

  private buildQueryFromRequest(request: KsAgGridServerRequest): SportListQuery {
    const sort = request.sortModel?.[0];
    const name = this.readTextFilter(request.filterModel?.['name']);
    const active = this.readBooleanFilter(request.filterModel?.['active']);

    return {
      ...(name ? { name } : {}),
      ...(active !== undefined ? { active } : {}),
      ...(sort?.colId ? { sortBy: sort.colId } : {}),
      ...(sort?.sort === 'asc' || sort?.sort === 'desc' ? { sortOrder: sort.sort } : {}),
    };
  }

  private readTextFilter(filterModel: unknown): string | undefined {
    if (!filterModel || typeof filterModel !== 'object') {
      return undefined;
    }

    const filterValue = (filterModel as { filter?: unknown }).filter;
    return typeof filterValue === 'string' && filterValue.trim() ? filterValue.trim() : undefined;
  }

  private readBooleanFilter(filterModel: unknown): boolean | undefined {
    if (!filterModel || typeof filterModel !== 'object') {
      return undefined;
    }

    const rawValue = (filterModel as { filter?: unknown; value?: unknown }).filter ??
      (filterModel as { value?: unknown }).value;

    if (typeof rawValue === 'boolean') {
      return rawValue;
    }

    if (typeof rawValue !== 'string') {
      return undefined;
    }

    const normalizedValue = rawValue.trim().toLowerCase();
    if (normalizedValue === 'true' || normalizedValue === 'yes' || normalizedValue === '1') {
      return true;
    }

    if (normalizedValue === 'false' || normalizedValue === 'no' || normalizedValue === '0') {
      return false;
    }

    return undefined;
  }

  onNewSportClick(): void {
    const name = window.prompt('Enter sport name');
    if (!name || !name.trim()) {
      return;
    }

    this.sportService.createSport({ name: name.trim(), active: true }).subscribe({
      next: () => this.refreshServerDataSource(),
      error: () => {
        this.error = 'Failed to create sport.';
      },
    });
  }
}
