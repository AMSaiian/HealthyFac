export const TITLE_MAX_LENGTH = 200;
export const DESCRIPTION_MAX_LENGTH = 5000;
export const ESTIMATE_MIN_MINUTES = 5;
export const ESTIMATE_MAX_MINUTES = 720;
export const ESTIMATE_STEP_MINUTES = 5;
/** The grid of planned starts in minutes. The backend rounds every start and end to it. */
export const GRID_STEP_MINUTES = 5;
/** The unit text that follows an estimate in minutes. */
export const ESTIMATE_SUFFIX = ' min';
export const FILE_MAX_COUNT = 20;
/** The rows of one page of the drawer history. */
export const HISTORY_PAGE_SIZE = 10;
/** The most changes that one apply of the schedule tab takes. The backend checks the same limit. */
export const SCHEDULE_APPLY_MAX_CHANGES = 100;
/** The backlog work orders that the staging area of the schedule tab loads. */
export const STAGING_LIMIT = 100;
