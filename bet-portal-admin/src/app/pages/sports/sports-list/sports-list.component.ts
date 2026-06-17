import { Component, inject, OnInit, PLATFORM_ID, NO_ERRORS_SCHEMA, ViewChild } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { AllCommunityModule, ColDef, GridApi, IDatasource, IGetRowsParams, ModuleRegistry, themeAlpine } from 'ag-grid-community';
import { KsAgGridModule, KsAgGridServerRequest, KsAgGridComponent } from 'ks-ag-controls';
import { Sport, SportListQuery, SportService } from '../../../services/sport.service';
import { ThemeService } from '../../../services/theme.service';
import { GridSettingsService, GridSettingsData } from '../../../services/grid-settings.service';

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

  @ViewChild('agGrid') agGridComponent?: KsAgGridComponent<Sport>;

  private gridApi?: GridApi;
  private gridInitialized = false;
  private readonly pageIdentifier = 'sports-list';
  private loadedSettings?: GridSettingsData;
  initialPageSize = 25;
  private newRowBeingAdded: Partial<Sport> | null = null;
  private cellChanges = new Map<number | string, Partial<Sport>>();

  error = '';
  readonly isBrowser = isPlatformBrowser(this.platformId);
  readonly modules = [AllCommunityModule];
  readonly theme = themeAlpine;

  readonly columnDefs: ColDef<Sport>[] = [
    { field: 'name', headerName: 'Sport', flex: 20, minWidth: 160, sortable: true, filter: true, editable: true },
    {
      field: 'active',
      headerName: 'Active',
      width: 110,
      sortable: true,
      filter: true,
      editable: true,
      cellEditor: 'agSelectCellEditor',
      cellEditorParams: {
        values: [true, false],
      },
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

    // Apply saved settings to grid if available
    this.applyGridSettings();
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

  onAddNewItem(): void {
    // If there's already a new row being added, don't add another
    if (this.newRowBeingAdded) {
      return;
    }

    // Create a new blank sport object (no id means it's a new row)
    const newSport: Partial<Sport> = {
      name: '',
      active: true,
    };

    this.newRowBeingAdded = newSport;

    // Get the grid API
    const gridApi = this.agGridComponent?.agGrid?.api;
    if (!gridApi) {
      console.warn('Grid API not available');
      return;
    }

    // Add the new row as a pinned bottom row (works with server-side data)
    gridApi.setGridOption('pinnedBottomRowData', [newSport]);

    // Enter edit mode for the new row
    if (this.agGridComponent) {
      this.agGridComponent.enterEditMode(newSport as Sport);
    }

    // Focus on the first cell after a short delay
    setTimeout(() => {
      const firstEditableCol = this.columnDefs.find(col => col.editable);
      if (firstEditableCol && firstEditableCol.field) {
        gridApi.setFocusedCell(0, firstEditableCol.field, 'bottom');
        gridApi.startEditingCell({
          rowIndex: 0,
          colKey: firstEditableCol.field,
          rowPinned: 'bottom',
        });
      }
    }, 100);
  }

  onSettingsSaved(gridState: any): void {
    this.saveGridSettings(gridState);
  }

  onSettingsCleared(): void {
    this.gridSettingsService.deleteSettings(this.pageIdentifier).subscribe({
      next: () => {
        this.error = '';

        // Clear loaded settings
        this.loadedSettings = undefined;

        // Reset grid to default state
        if (this.gridApi) {
          // Reset column state to defaults
          this.gridApi.resetColumnState();

          // Clear all filters
          this.gridApi.setFilterModel(null);

          // Reset page size to initial value
          this.gridApi.setGridOption('paginationPageSize', 25);

          // Refresh data source to apply cleared filters
          this.refreshServerDataSource();
        }
      },
      error: () => {
        this.error = 'Failed to clear grid settings';
      },
    });
  }

  handleRowSaved(sport: Sport): void {
    // Apply any tracked cell changes to the sport object
    const sportId = sport.id || 'new';
    const changes = this.cellChanges.get(sportId);
    if (changes) {
      Object.assign(sport, changes);
    }

    if (!sport.id) {
      // New row - create
      this.sportService.createSport({ name: sport.name, active: sport.active }).subscribe({
        next: () => {
          this.error = '';

          // Clear tracked changes
          this.cellChanges.delete('new');

          // Clear the pinned row if this was a new row being added
          if (this.newRowBeingAdded) {
            this.gridApi?.setGridOption('pinnedBottomRowData', []);
            this.newRowBeingAdded = null;
          }

          this.refreshServerDataSource();
        },
        error: () => {
          this.error = 'Failed to create sport.';
        },
      });
    } else {
      // Existing row - update
      this.sportService.updateSport(sport.id, { name: sport.name, active: sport.active }).subscribe({
        next: () => {
          this.error = '';

          // Clear tracked changes for this row
          this.cellChanges.delete(sport.id);

          this.refreshServerDataSource();
        },
        error: () => {
          this.error = 'Failed to update sport.';
        },
      });
    }
  }

  handleRowDeleted(sport: Sport): void {
    if (!sport.id) {
      // If this is the new row being added, clear it from pinned rows
      if (this.newRowBeingAdded) {
        this.gridApi?.setGridOption('pinnedBottomRowData', []);
        this.newRowBeingAdded = null;
      }

      return;
    }

    this.sportService.deleteSport(sport.id).subscribe({
      next: () => {
        this.error = '';
        this.refreshServerDataSource();
      },
      error: () => {
        this.error = 'Failed to delete sport.';
        this.error = 'Failed to delete sport.';
      },
    });
  }

  handleCellChanged(event: any): void {
    if (!event?.data || !event?.field) {
      return;
    }

    // Track changes for this row
    const sportId: number | string = event.data.id || 'new';
    const changes = this.cellChanges.get(sportId) || {};
    (changes as any)[event.field] = event.newValue;
    this.cellChanges.set(sportId, changes);
  }
}
