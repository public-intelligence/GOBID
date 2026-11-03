export interface PositionClaim {
  positionClaimId: number;
  positionId: string;
  claimId: number;
  claimName: string;
  claimCode: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface PositionClaimAdapted {
  id: number;
  positionId: string;
  claimId: number;
  claimName: string;
  claimCode: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreatePositionClaim {
  positionId: string;
  claimId: number;
  isActive: boolean;
}
