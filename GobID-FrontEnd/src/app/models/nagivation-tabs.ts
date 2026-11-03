interface Tab {
  id: number;
  title: string;
  name: string;
}

export interface NavigationTabs {
  selected: string;
  tabs: Tab[];
}
