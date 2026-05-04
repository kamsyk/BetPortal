import { Component, inject, OnInit, PLATFORM_ID } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { AgGridAngular } from 'ag-grid-angular';
import { AllCommunityModule, ColDef, ModuleRegistry, themeAlpine } from 'ag-grid-community';
import { KsAgGridModule, KsAgGridServerRequest, KsAgGridServerRowsFetcher } from 'ks-ag-controls';
import { Sport, SportService } from '../../../services/sport.service';
import { map } from 'rxjs';

ModuleRegistry.registerModules([AllCommunityModule]);

@Component({
  selector: 'app-sports-list',
  standalone: true,
  imports: [CommonModule, AgGridAngular, KsAgGridModule],
  templateUrl: './sports-list.component.html',
  styleUrl: './sports-list.component.scss'
})
export class SportsListComponent implements OnInit {
  private readonly sportService = inject(SportService);
  private readonly platformId = inject(PLATFORM_ID);

  sports: Sport[] = [];
  loading = false;
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

  ngOnInit(): void {
    if (!this.isBrowser) {
      return;
    }

    this.loadSports();
  }

  loadSports(): void {
    this.loading = true;
    this.error = '';

    this.sportService.getSports().subscribe({
      next: (sports) => {
        this.sports = sports;
        this.loading = false;
      },
      error: () => {
        this.error = 'Failed to load sports.';
        this.loading = false;
      },
    });
  }

  readonly fetchServerRows: KsAgGridServerRowsFetcher<Sport> = (request: KsAgGridServerRequest) => {
    return this.sportService.getSports().pipe(
      map((sports) => ({
        rows: sports.slice(request.startRow, request.endRow),
        lastRow: sports.length,
      }))
    );
  };

  onNewSportClick(): void {
    const name = window.prompt('Enter sport name');
    if (!name || !name.trim()) {
      return;
    }

    this.sportService.createSport({ name: name.trim(), active: true }).subscribe({
      next: () => this.loadSports(),
      error: () => {
        this.error = 'Failed to create sport.';
      },
    });
  }
}
