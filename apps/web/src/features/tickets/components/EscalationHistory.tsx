import { formatDateTime } from '../../../lib/format';
import type { TicketEscalation } from '../../../types/api';

interface EscalationHistoryProps {
  entries: TicketEscalation[];
  isLoading: boolean;
  error?: string;
  onRetry: () => void;
}

/** Presents the immutable escalation records from newest to oldest. */
export function EscalationHistory({ entries, isLoading, error, onRetry }: EscalationHistoryProps) {
  return (
    <section className="card" aria-labelledby="escalation-history-heading">
      <h2 id="escalation-history-heading">Escalation history</h2>

      {isLoading && <p className="field__hint" role="status">Loading escalation history...</p>}

      {error && (
        <div className="error" role="alert">
          <p>{error}</p>
          <button type="button" className="button button--ghost" onClick={onRetry}>
            Try again
          </button>
        </div>
      )}

      {!isLoading && !error && entries.length === 0 && (
        <p className="field__hint">No escalations have been recorded.</p>
      )}

      {!error && entries.length > 0 && (
        <ol className="escalation-history" aria-label="Escalation records, newest first">
          {entries.map((entry) => (
            <li className="escalation-history__item" key={entry.id}>
              <div className="escalation-history__heading">
                <strong>{entry.fromPriority} → {entry.toPriority}</strong>
                <time dateTime={entry.escalatedAtUtc}>{formatDateTime(entry.escalatedAtUtc)}</time>
              </div>
              <dl className="escalation-history__details">
                <div>
                  <dt>Assigned agent</dt>
                  <dd>{entry.fromAgent?.fullName ?? 'Unassigned'} → {entry.toAgent?.fullName ?? 'Unassigned'}</dd>
                </div>
                <div>
                  <dt>Due date</dt>
                  <dd>{formatDateTime(entry.fromDueAtUtc)} → {formatDateTime(entry.toDueAtUtc)}</dd>
                </div>
                <div>
                  <dt>Reason</dt>
                  <dd>{entry.reason}</dd>
                </div>
                <div>
                  <dt>Escalated by</dt>
                  <dd>{entry.escalatedBy}</dd>
                </div>
              </dl>
            </li>
          ))}
        </ol>
      )}
    </section>
  );
}
