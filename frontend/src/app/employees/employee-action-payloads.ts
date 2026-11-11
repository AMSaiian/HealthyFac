import { type infer as Infer, number, object, optional, string } from 'zod/mini';

/** The payload of an employee action: the employee or the invite that the action is about. */
export const employeeActionPayloadSchema = object({
  employeeId: optional(string()),
  inviteId: optional(string()),
  rowVersion: optional(number()),
});

export type EmployeeActionPayload = Infer<typeof employeeActionPayloadSchema>;
