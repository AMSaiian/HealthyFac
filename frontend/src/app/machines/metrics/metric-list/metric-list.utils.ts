import { MetricMarker } from '@machines/metrics/metrics-page-store/metrics-page.store.utils';

export const MARKER_LABEL_KEYS: Readonly<Record<MetricMarker, string>> = {
  [MetricMarker.New]: 'METRICS.MARKERS.NEW',
  [MetricMarker.Edited]: 'METRICS.MARKERS.EDITED',
  [MetricMarker.Deleted]: 'METRICS.MARKERS.DELETED',
  [MetricMarker.Invalid]: 'METRICS.MARKERS.INVALID',
};

export const MARKER_SEVERITIES: Readonly<Record<MetricMarker, 'success' | 'info' | 'danger' | 'warn'>> = {
  [MetricMarker.New]: 'success',
  [MetricMarker.Edited]: 'info',
  [MetricMarker.Deleted]: 'danger',
  [MetricMarker.Invalid]: 'warn',
};
