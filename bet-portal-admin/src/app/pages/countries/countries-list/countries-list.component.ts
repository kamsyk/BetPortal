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
  private newRowBeingAdded: Partial<Country> | null = null;
  private cellChanges = new Map<number | string, Partial<Country>>();

  error = '';
  readonly isBrowser = isPlatformBrowser(this.platformId);
  readonly modules = [AllCommunityModule];
  readonly theme = themeAlpine;

  readonly columnDefs: ColDef<Country>[] = [
    { field: 'name', headerName: 'Country', flex: 20, minWidth: 160, sortable: true, filter: true, editable: true },
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

  onAddNewItem(): void {
    // If there's already a new row being added, don't add another
    if (this.newRowBeingAdded) {
      return;
    }

    // Create a new blank country object (no id means it's a new row)
    const newCountry: Partial<Country> = {
      name: '',
      active: true,
    };

    this.newRowBeingAdded = newCountry;

    // Get the grid API
    const gridApi = this.agGridComponent?.agGrid?.api;
    if (!gridApi) {
      console.warn('Grid API not available');
      return;
    }

    // Add the new row as a pinned bottom row (works with server-side data)
    gridApi.setGridOption('pinnedBottomRowData', [newCountry]);

    // Enter edit mode for the new row
    if (this.agGridComponent) {
      this.agGridComponent.enterEditMode(newCountry as Country);
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

  handleRowSaved(country: Country): void {
    console.log('handleRowSaved called with:', country);

    // Apply any tracked cell changes to the country object
    const countryId: number | string = country.id || 'new';
    const changes = this.cellChanges.get(countryId);
    if (changes) {
      Object.assign(country, changes);
      console.log('Applied changes:', changes, 'Final country:', country);
    }

    if (!country.id) {
      // New row - create
      this.countryService.createCountry({ name: country.name, active: country.active }).subscribe({
        next: (createdCountry) => {
          console.log('Country created:', createdCountry);
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
        error: (err) => {
          console.error('Failed to create country:', err);
          this.error = 'Failed to create country.';
        },
      });
    } else {
      // Existing row - update
      this.countryService.updateCountry(country.id, { name: country.name, active: country.active }).subscribe({
        next: (updatedCountry) => {
          console.log('Country updated:', updatedCountry);
          this.error = '';

          // Clear tracked changes for this row
          this.cellChanges.delete(country.id);

          this.refreshServerDataSource();
        },
        error: (err) => {
          console.error('Failed to update country:', err);
          this.error = 'Failed to update country.';
        },
      });
    }
  }

  handleRowDeleted(country: Country): void {
    console.log('handleRowDeleted called with:', country);

    if (!country.id) {
      // If this is the new row being added, clear it from pinned rows
      if (this.newRowBeingAdded) {
        this.gridApi?.setGridOption('pinnedBottomRowData', []);
        this.newRowBeingAdded = null;
      }

      console.log('Cannot delete unsaved row');
      return;
    }

    this.countryService.deleteCountry(country.id).subscribe({
      next: () => {
        console.log('Country deleted:', country.id);
        this.error = '';
        this.refreshServerDataSource();
      },
      error: (err) => {
        console.error('Failed to delete country:', err);
        this.error = 'Failed to delete country.';
      },
    });
  }

  handleCellChanged(event: any): void {
    console.log('Cell value changed:', event);

    if (!event?.data || !event?.field) {
      return;
    }

    // Track changes for this row
    const countryId: number | string = event.data.id || 'new';
    const changes = this.cellChanges.get(countryId) || {};
    (changes as any)[event.field] = event.newValue;
    this.cellChanges.set(countryId, changes);

    console.log('Tracked changes:', this.cellChanges);
  }
}
