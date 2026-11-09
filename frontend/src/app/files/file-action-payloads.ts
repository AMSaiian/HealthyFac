import { type infer as Infer, literal, nullable, number, object, string, union } from 'zod/mini';

const scopeSchema = union([
  object({ kind: literal('company') }),
  object({ kind: literal('machine'), machineId: string() }),
]);

/** The payload of the action `files.upload`: where the files go. */
export const fileUploadActionPayloadSchema = object({ scope: scopeSchema });

export type FileUploadActionPayload = Infer<typeof fileUploadActionPayloadSchema>;

/** The payload of the action `files.edit`: the scope and the grid row that the user edits. */
export const fileEditActionPayloadSchema = object({
  scope: scopeSchema,
  item: object({
    id: string(),
    fileName: string(),
    extension: string(),
    description: nullable(string()),
    rowVersion: number(),
  }),
});

export type FileEditActionPayload = Infer<typeof fileEditActionPayloadSchema>;
