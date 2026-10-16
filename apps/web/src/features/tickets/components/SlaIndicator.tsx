import { Badge, type BadgeTone } from '../../../components/Badge';
import type { SlaStatus } from '../../../types/api';

const states: Record<SlaStatus, { label: string; tone: BadgeTone }> = {
  WithinSla: { label: 'Within SLA', tone: 'info' },
  AtRisk: { label: 'At risk', tone: 'warning' },
  Breached: { label: 'Breached', tone: 'danger' },
  Met: { label: 'Met', tone: 'success' },
  NotApplicable: { label: 'No SLA', tone: 'neutral' },
};

/** Shows the SLA state with text as well as a visual badge tone. */
export function SlaIndicator({ status }: { status: SlaStatus }) {
  const state = states[status];
  return <Badge tone={state.tone}>{state.label}</Badge>;
}
