import { Component, inject, OnInit, PLATFORM_ID, NO_ERRORS_SCHEMA, ViewChild } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { AllCommunityModule, ColDef, GridApi, IDatasource, IGetRowsParams, ModuleRegistry, themeAlpine } from 'ag-grid-community';
import { KsAgGridModule, KsAgGridServerRequest, KsAgGridComponent } from 'ks-ag-controls';
import { Country, CountryListQuery, CountryService } from '../../../services/country.service';
import { ThemeService } from '../../../services/theme.service';
import { GridSettingsService, GridSettingsData } from '../../../services/grid-settings.service';

ModuleRegistry.registerModules([AllCommunityModule]);

@Component({
  selector: 'app-countries-list',
  standalone: true,
  imports: [CommonModule, KsAgGridModule],
  schemas: [NO_ERRORS_SCHEMA],
  templateUrl: './countries-list.component.html',
  styleUrl: './countries-list.component.scss'
})
export class CountriesListComponent implements OnInit {
  private readonly countryService = inject(CountryService);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly gridSettingsService = inject(GridSettingsService);
  readonly themeService = inject(ThemeService);

  @ViewChild('agGrid') agGridComponent?: KsAgGridComponent<Country>;

  private gridApi?: GridApi;
  private gridInitialized = false;
  private readonly pageIdentifier = 'countries-list';
  private loadedSettings?: GridSettingsData;
  initialPageSize = 25;

  error = '';
  readonly isBrowser = isPlatformBrowser(this.platformId);
  readonly modules = [AllCommunityModule];
  readonly theme = themeAlpine;

  readonly columnDefs: ColDef<Country>[] = [
    { field: 'name', headerName: 'Country', flex: 20, minWidth: 160, sortable: true, filter: true },
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

    // Load saved settings on initialization
    this.loadGridSettings();
  }

  private loadGridSettings(): void {
    this.gridSettingsService.getSettings(this.pageIdentifier).subscribe({
      next: (response) => {
        if (response.settingsJson) {
          this.loadedSettings = JSON.parse(response.settingsJson);
          if (this.loadedSettings && this.loadedSettings.pageSize) {
            this.initialPageSize = this.loadedSettings.pageSize;
          }
          // If grid is already initialized, apply settings now
          if (this.gridInitialized) {
            this.applyGridSettings();
          }
        }
      },
      error: () => {
        // Settings not found or error loading - use defaults
      },
    });
  }

  private applyGridSettings(): void {
    if (!this.gridApi || !this.loadedSettings) {
      return;
    }

    console.log('Applying grid settings:', this.loadedSettings);

    // Apply column state (includes visibility, order, width, etc.)
    if (this.loadedSettings.columnState && this.loadedSettings.columnState.length > 0) {
      this.gridApi.applyColumnState({ state: this.loadedSettings.columnState, applyOrder: true });
    }

    // Apply filter model
    if (this.loadedSettings.filterModel) {
      this.gridApi.setFilterModel(this.loadedSettings.filterModel);
    }

    // Apply page size
    if (this.loadedSettings.pageSize) {
      this.gridApi.setGridOption('paginationPageSize', this.loadedSettings.pageSize);
    }
  }

  private saveGridSettings(gridState: any): void {
    if (!this.gridApi) {
      return;
    }

    const settings: GridSettingsData = {
      pageSize: this.gridApi.paginationGetPageSize(),
      filterModel: gridState.filterModel,
      columnState: gridState.columnState,
    };

    this.gridSettingsService.saveSettings(this.pageIdentifier, settings).subscribe({
      next: () => {
        console.log('Grid settings saved successfully');
      },
      error: () => {
        this.error = 'Failed to save grid settings';
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

        this.countryService.getCountries(query).subscribe({
          next: (countries) => {
            params.successCallback(countries.slice(request.startRow, request.endRow), countries.length);
          },
          error: () => {
            this.error = 'Failed to load countries.';
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

    // Apply saved settings to grid if available
    this.applyGridSettings();
  }

  private refreshServerDataSource(): void {
    this.serverDataSource = this.createServerDataSource();
  }

  private buildQueryFromRequest(request: KsAgGridServerRequest): CountryListQuery {
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

  onNewCountryClick(): void {
    const name = window.prompt('Enter country name');
    if (!name || !name.trim()) {
      return;
    }

    this.countryService.createCountry({ name: name.trim(), active: true }).subscribe({
      next: () => this.refreshServerDataSource(),
      error: () => {
        this.error = 'Failed to create country.';
      },
    });
  }

  onSettingsSaved(gridState: any): void {
    this.saveGridSettings(gridState);
  }

  onSettingsCleared(): void {
    this.gridSettingsService.deleteSettings(this.pageIdentifier).subscribe({
      next: () => {
        console.log('Grid settings cleared successfully');
        this.error = '';

        // Clear loaded settings
        this.loadedSettings = undefined;

        // Reset grid to defaults
        if (this.gridApi) {
          this.gridApi.setFilterModel(null);
          this.gridApi.setGridOption('paginationPageSize', 25);
        }
      },
      error: () => {
        this.error = 'Failed to clear grid settings';
      },
    });
  }
}
