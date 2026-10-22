import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '../api';
import { ACTIVE_WORKSPACE_STORAGE_KEY, resolveActiveWorkspaceId, useWorkspaceStore } from './useWorkspaceStore';

const ws = (id: string) => ({ id, name: `Workspace ${id}`, createdAt: '2026-10-02T00:00:00Z' });

describe('resolveActiveWorkspaceId', () => {
  it('prefers the stored id when it still exists', () => {
    expect(resolveActiveWorkspaceId([ws('a'), ws('b')], 'b')).toBe('b');
  });

  it('falls back to the first workspace when the stored id is missing or invalid', () => {
    expect(resolveActiveWorkspaceId([ws('a'), ws('b')], null)).toBe('a');
    expect(resolveActiveWorkspaceId([ws('a'), ws('b')], 'gone')).toBe('a');
  });

  it('returns null when there are no workspaces', () => {
    expect(resolveActiveWorkspaceId([], 'a')).toBeNull();
  });
});

describe('useWorkspaceStore', () => {
  beforeEach(() => {
    const data = new Map<string, string>();
    vi.stubGlobal('localStorage', {
      getItem: (k: string) => data.get(k) ?? null,
      setItem: (k: string, v: string) => void data.set(k, v),
      clear: () => data.clear(),
    });
    useWorkspaceStore.setState({ workspaces: [], activeWorkspaceId: null, isLoaded: false, error: null });
  });
  afterEach(() => {
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
  });

  it('loads workspaces and restores the persisted selection', async () => {
    localStorage.setItem(ACTIVE_WORKSPACE_STORAGE_KEY, 'b');
    vi.spyOn(api.workspaces, 'getAll').mockResolvedValue([ws('a'), ws('b')]);

    await useWorkspaceStore.getState().loadWorkspaces();

    expect(useWorkspaceStore.getState().activeWorkspaceId).toBe('b');
    expect(useWorkspaceStore.getState().isLoaded).toBe(true);
  });

  it('reports zero workspaces so the first-workspace screen can be shown', async () => {
    vi.spyOn(api.workspaces, 'getAll').mockResolvedValue([]);

    await useWorkspaceStore.getState().loadWorkspaces();

    expect(useWorkspaceStore.getState().workspaces).toEqual([]);
    expect(useWorkspaceStore.getState().activeWorkspaceId).toBeNull();
  });

  it('switches to a newly created workspace and persists it', async () => {
    useWorkspaceStore.setState({ workspaces: [ws('a')], activeWorkspaceId: 'a', isLoaded: true });
    vi.spyOn(api.workspaces, 'create').mockResolvedValue(ws('c'));

    await useWorkspaceStore.getState().createWorkspace('C');

    expect(useWorkspaceStore.getState().activeWorkspaceId).toBe('c');
    expect(localStorage.getItem(ACTIVE_WORKSPACE_STORAGE_KEY)).toBe('c');
  });
});
