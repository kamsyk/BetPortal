# Grid Settings Feature

This feature allows users to save and restore their ag-grid settings (page size, filters, column visibility, column order) for each page.

## Database Changes

A new table `UserGridSettings` has been added to store user-specific grid settings.

### Creating the Migration

To create the database migration for the new `UserGridSettings` table:

#### For SQL Server (MsSql):
```bash
cd DbMigration.MsSql
dotnet ef migrations add AddUserGridSettings --context BpDbContext
```

#### For MySQL:
```bash
cd DbMigration.MySql
dotnet ef migrations add AddUserGridSettings --context BpDbContext
```

### Updating the Database

#### For Development (SQL Server):
```bash
cd BpDbLayer\EfUtils
UpdateDevelopLocal.bat
```

Or manually:
```bash
cd DbMigration.MsSql
dotnet ef database update --context BpDbContext
```

## API Endpoints

### Get Grid Settings
```
GET /api/gridsettings/{pageIdentifier}
```
Returns the saved grid settings for the current user and specified page.

**Response:** `GridSettingsDto`

### Save Grid Settings
```
POST /api/gridsettings
```
Saves or updates grid settings for the current user and specified page.

**Request Body:**
```json
{
  "pageIdentifier": "sports-list",
  "settings": {
    "pageSize": 50,
    "filterModel": { ... },
    "columnState": [
      {
        "colId": "name",
        "hide": false,
        "width": 200,
        "sort": "asc"
      }
    ]
  }
}
```

**Response:** `GridSettingsDto`

### Delete Grid Settings
```
DELETE /api/gridsettings/{pageIdentifier}
```
Deletes the saved grid settings for the current user and specified page.

## Client-Side Usage

### Service

The `GridSettingsService` provides methods to interact with the API:

```typescript
import { GridSettingsService } from './services/grid-settings.service';

// Get settings
this.gridSettingsService.getSettings('sports-list').subscribe(settings => {
  // Apply settings
});

// Save settings
this.gridSettingsService.saveSettings('sports-list', {
  pageSize: 50,
  filterModel: filterModel,
  columnState: columnState
}).subscribe();

// Delete settings
this.gridSettingsService.deleteSettings('sports-list').subscribe();
```

### Component Integration

The `sports-list.component.ts` demonstrates how to integrate grid settings:

1. **Load settings on initialization**
2. **Apply settings when grid is ready**
3. **Save settings when grid state changes** (debounced to avoid excessive API calls)

```typescript
export class SportsListComponent implements OnInit {
  private readonly gridSettingsService = inject(GridSettingsService);
  private gridApi?: GridApi;
  private readonly pageIdentifier = 'sports-list';
  private readonly settingsChange$ = new Subject<void>();
  private initialPageSize = 25;

  ngOnInit(): void {
    // Load saved settings
    this.loadGridSettings();

    // Setup debounced settings save
    this.settingsChange$.pipe(debounceTime(1000)).subscribe(() => {
      this.saveGridSettings();
    });
  }

  onGridReady(event: GridReadyEvent): void {
    this.gridApi = event.api;
    this.applyGridSettings();

    // Listen to state changes
    this.gridApi.addEventListener('columnResized', () => this.settingsChange$.next());
    this.gridApi.addEventListener('columnMoved', () => this.settingsChange$.next());
    this.gridApi.addEventListener('columnVisible', () => this.settingsChange$.next());
    this.gridApi.addEventListener('sortChanged', () => this.settingsChange$.next());
    this.gridApi.addEventListener('filterChanged', () => this.settingsChange$.next());
  }
}
```

## Stored Settings

The following grid settings are automatically saved:

- **Page Size**: Number of rows per page
- **Filter Model**: All applied filters
- **Column State**: 
  - Column visibility (hide/show)
  - Column width
  - Column order
  - Column sorting
  - Column pinning

## Adding Grid Settings to Other Pages

To add grid settings to other pages:

1. Inject `GridSettingsService` in your component
2. Choose a unique `pageIdentifier` (e.g., 'users-list', 'reports-list')
3. Follow the same pattern as `sports-list.component.ts`:
   - Load settings in `ngOnInit()`
   - Apply settings in `onGridReady()`
   - Save settings on state changes (with debounce)

## Database Schema

```sql
CREATE TABLE UserGridSettings (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL,
    PageIdentifier NVARCHAR(100) NOT NULL,
    SettingsJson NVARCHAR(MAX) NULL,
    CreatedDate DATETIME2 NOT NULL,
    CreatedByUserId INT NOT NULL,
    ModifiedDate DATETIME2 NOT NULL,
    ModifiedByUserId INT NOT NULL,
    CONSTRAINT FK_UserGridSettings_User FOREIGN KEY (UserId) REFERENCES BpUsers(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_UserGridSettings_UserPage UNIQUE (UserId, PageIdentifier)
);
```

## Notes

- Settings are user-specific (each user has their own settings)
- Settings are page-specific (identified by `pageIdentifier`)
- Settings are saved automatically after 1 second of inactivity (debounced)
- Settings include column state, filters, and page size
- If no settings are found, default values are used
