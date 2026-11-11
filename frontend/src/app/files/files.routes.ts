import type { Routes } from '@angular/router';

import { IngestApi } from './ingest/ingest-api';
import { FilesApi } from './files-api';
import { FilesGridStore } from './list/files-grid.store';
import { CompanyFilesPage } from './list/company-files-page/company-files-page';

export const FILES_ROUTES: Routes = [
  { path: '', component: CompanyFilesPage, providers: [FilesApi, IngestApi, FilesGridStore] },
];
