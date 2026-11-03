export interface LegalEntity {
  id: string;
  legalName: string;
  socialName: string;
  identificationNumber: string;
  email: string;
  phoneNumber: string;
  type: string;
  status: string;
}

export interface LegalEntityAdapted {
  id: string;
  legalName: string;
  socialName: string;
  identificationNumber: string;
  email: string;
  phoneNumber: string;
  type: string;
  status: string;
}

export interface LegalEntityCreate {
  id?: string;
  legalName: string;
  socialName: string;
  rnc: string;
  email: string;
  phoneNumber: string;
  type: string;
  status: string;
}

export interface LegalEntityFilter {
  search?: string;
  pageNumber: number;
  pageSize: number;
}
