export enum EmployeeRole {
  Worker = 'worker',
  Supervisor = 'supervisor',
}

export interface CurrentEmployee {
  readonly id: string;
  readonly firstName: string;
  readonly lastName: string;
  readonly roleCode: EmployeeRole;
}
