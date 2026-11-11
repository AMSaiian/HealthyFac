export const EmployeeActionType = {
  Invite: 'employees.invite',
  Resend: 'employees.resend',
  RetryProvisioning: 'employees.retry-provisioning',
  Revoke: 'employees.revoke',
  Saved: 'employees.saved',
} as const;

/** The detail text of the `409` that the backend sends when an open invite for the email exists. */
export const OPEN_INVITE_EXISTS_DETAIL = 'An open invite for this email already exists in the company.';
