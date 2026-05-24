import { Component, inject, OnInit, PLATFORM_ID, NO_ERRORS_SCHEMA } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { AllCommunityModule, ColDef, IDatasource, IGetRowsParams, ModuleRegistry, themeAlpine } from 'ag-grid-community';
import { KsAgGridModule, KsAgGridServerRequest } from 'ks-ag-controls';
import { Sport, SportListQuery, SportService } from '../../../services/sport.service';
import { ThemeService } from '../../../services/theme.service';

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
  readonly themeService = inject(ThemeService);

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
  }

  private createServerDataSource(): IDatasource {
    return {
      getRows: (params: IGetRowsParams) => {
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
