import { useState, type FormEvent } from 'react';
import { toErrorMessage } from '../../../api/client';
import { ticketsApi } from '../../../api/tickets';
import { Field } from '../../../components/Field';
import type { TicketDetail, TicketStatus } from '../../../types/api';

interface EscalationPanelProps {
  ticket: TicketDetail;
  onTicketUpdated: (ticket: TicketDetail) => void;
  onRefresh: () => Promise<void>;
}

const finalStatuses: TicketStatus[] = ['Resolved', 'Closed'];
const MIN_REASON_LENGTH = 5;
const MAX_REASON_LENGTH = 500;

/** Validates and submits a ticket escalation, preserving the current ticket on API errors. */
export function EscalationPanel({ ticket, onTicketUpdated, onRefresh }: EscalationPanelProps) {
  const [reason, setReason] = useState('');
  const [reasonError, setReasonError] = useState<string>();
  const [serverError, setServerError] = useState<string>();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const canEscalate = ticket.priority !== 'Critical' && !finalStatuses.includes(ticket.status);

  const submit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const trimmedReason = reason.trim();

    if (trimmedReason.length < MIN_REASON_LENGTH || trimmedReason.length > MAX_REASON_LENGTH) {
      setReasonError(`Enter a reason between ${MIN_REASON_LENGTH} and ${MAX_REASON_LENGTH} characters.`);
      return;
    }

    setReasonError(undefined);
    setServerError(undefined);
    setIsSubmitting(true);

    try {
      const response = await ticketsApi.escalateTicket(ticket.id, trimmedReason);
      onTicketUpdated(response.ticket);
      setReason('');
      await onRefresh();
    } catch (error) {
      setServerError(toErrorMessage(error, 'Could not escalate the ticket.'));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <section className="card" aria-labelledby="escalation-heading">
      <h2 id="escalation-heading">Escalate ticket</h2>

      {!canEscalate && (
        <p className="field__hint">
          {ticket.priority === 'Critical'
            ? 'Critical tickets cannot be escalated.'
            : `${ticket.status} tickets cannot be escalated.`}
        </p>
      )}

      <form className="form" onSubmit={(event) => void submit(event)} noValidate>
        <Field
          id="escalation-reason"
          label="Reason for escalation"
          hint="Enter 5–500 characters explaining why this ticket needs a higher priority."
          error={reasonError}
        >
          {(fieldProps) => (
            <textarea
              {...fieldProps}
              rows={4}
              maxLength={MAX_REASON_LENGTH}
              value={reason}
              disabled={!canEscalate || isSubmitting}
              onChange={(event) => {
                setReason(event.target.value);
                setReasonError(undefined);
                setServerError(undefined);
              }}
            />
          )}
        </Field>

        <button
          type="submit"
          className="button button--primary"
          disabled={!canEscalate || isSubmitting}
        >
          {isSubmitting ? 'Escalating...' : 'Escalate ticket'}
        </button>
        <p className="field__hint">This escalation will be recorded as Support agent until sign-in is added.</p>
      </form>

      {serverError && <p className="field__error" role="alert">{serverError}</p>}
    </section>
  );
}
