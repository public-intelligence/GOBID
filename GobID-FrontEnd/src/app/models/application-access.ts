export interface ApplicationAccess {
  applicationAccessId: string;
  organizationApplicationId: string;
  accessTypeId: string;
  configurationJson: string;
  isActive: boolean;
  createdAt: string;
  createdBy: string | null;
  updatedAt: string | null;
  updatedBy: string | null;
}

export interface ApplicationAccessAdapted {
  applicationAccessId: string;
  organizationApplicationId: string;
  accessTypeId: string;
  isActive: boolean;
  configurationJson: string;
  createdAt: string;
  createdBy: string | null;
  updatedAt: string | null;
  updatedBy: string | null;
}

export interface ApplicationAccessCreate {
  applicationAccessId?: string;
  organizationApplicationId: string;
  accessTypeId: string;
  isActive: boolean;
  configurationJson: string;
}
