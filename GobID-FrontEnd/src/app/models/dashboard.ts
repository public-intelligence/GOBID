export interface DashboardCard {
  id: string;
  title: string;
  value: string | number;
  subtitle?: string;
  icon?: string;
  trend?: 'up' | 'down' | 'neutral';
  trendValue?: string;
  color?: 'primary' | 'success' | 'warning' | 'danger' | 'info';
}

export interface ChartData {
  label: string;
  value: number;
  color?: string;
}

export interface monthlyActivity {
  monthNumber: number;
  monthName: string;
  successfulLogins: number;
}
export interface DashboardChart {
  id: string;
  title: string;
  type: 'bar' | 'line' | 'pie' | 'doughnut';
  data: ChartData[];
  description?: string;
}

export interface DashboardActivity {
  id: string;
  title: string;
  description: string;
  timestamp: Date;
  type: 'info' | 'success' | 'warning' | 'error';
  user?: string;
}

export interface DashboardStats {
  totalUsers: number;
  totalOrganizations?: number;
  totalApplications: number;
  systemHealth?: 'excellent' | 'good' | 'warning' | 'critical';
}

export interface DashboardData {
  cards: DashboardCard[];
  charts: DashboardChart[];
  stats: DashboardStats;
}
