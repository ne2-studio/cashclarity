import { create } from 'zustand';
import type { Workspace } from '../types';
import { api, setActiveWorkspaceId } from '../api';

export const ACTIVE_WORKSPACE_STORAGE_KEY = 'cashclarity.activeWorkspaceId';

function readStoredId(): string | null {
  try {
    return localStorage.getItem(ACTIVE_WORKSPACE_STORAGE_KEY);
  } catch {
    return null;
  }
}

function persistId(id: string) {
  try {
    localStorage.setItem(ACTIVE_WORKSPACE_STORAGE_KEY, id);
  } catch {
    // Persisting the last selection is best-effort.
  }
}

/** The stored id wins when it still exists; otherwise fall back to the first workspace. */
export function resolveActiveWorkspaceId(workspaces: Workspace[], storedId: string | null): string | null {
  if (storedId && workspaces.some((w) => w.id === storedId)) return storedId;
  return workspaces[0]?.id ?? null;
}

interface WorkspaceStore {
  workspaces: Workspace[];
  activeWorkspaceId: string | null;
  isLoaded: boolean;
  error: string | null;

  loadWorkspaces: () => Promise<void>;
  selectWorkspace: (id: string) => void;
  createWorkspace: (name: string) => Promise<Workspace>;
}

export const useWorkspaceStore = create<WorkspaceStore>((set, get) => ({
  workspaces: [],
  activeWorkspaceId: null,
  isLoaded: false,
  error: null,

  loadWorkspaces: async () => {
    set({ error: null });
    try {
      const workspaces = await api.workspaces.getAll();
      const activeWorkspaceId = resolveActiveWorkspaceId(workspaces, readStoredId());
      setActiveWorkspaceId(activeWorkspaceId ?? undefined);
      if (activeWorkspaceId) persistId(activeWorkspaceId);
      set({ workspaces, activeWorkspaceId, isLoaded: true });
    } catch (error) {
      set({ error: (error as Error).message, isLoaded: true });
    }
  },

  selectWorkspace: (id) => {
    if (!get().workspaces.some((w) => w.id === id)) return;
    setActiveWorkspaceId(id);
    persistId(id);
    set({ activeWorkspaceId: id });
  },

  createWorkspace: async (name) => {
    const workspace = await api.workspaces.create(name);
    set((state) => ({ workspaces: [...state.workspaces, workspace] }));
    get().selectWorkspace(workspace.id);
    return workspace;
  },
}));
