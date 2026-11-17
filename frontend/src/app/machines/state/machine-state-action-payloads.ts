import { type infer as Infer, object, string } from 'zod/mini';

/** The payload of the action `machines.change-state`: the machine whose state the dialog changes. */
export const machineStateActionPayloadSchema = object({ machineId: string() });

export type MachineStateActionPayload = Infer<typeof machineStateActionPayloadSchema>;
