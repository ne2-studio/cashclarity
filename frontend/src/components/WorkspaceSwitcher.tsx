import { useEffect, useRef, useState } from 'react';
import { Check, ChevronDown, Plus } from 'lucide-react';
import { cx } from '../design-system';
import type { Workspace } from '../types';
import { CreateWorkspaceModal } from './CreateWorkspaceModal';

interface WorkspaceSwitcherProps {
  workspaces: Workspace[];
  activeWorkspaceId: string | null;
  onSelect: (id: string) => void;
  onCreate: (name: string) => Promise<void>;
}

export function WorkspaceSwitcher({ workspaces, activeWorkspaceId, onSelect, onCreate }: WorkspaceSwitcherProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [isCreating, setIsCreating] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);
  const active = workspaces.find((w) => w.id === activeWorkspaceId);

  useEffect(() => {
    if (!isOpen) return;
    const onPointerDown = (e: MouseEvent) => {
      if (!containerRef.current?.contains(e.target as Node)) setIsOpen(false);
    };
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setIsOpen(false);
    };
    document.addEventListener('mousedown', onPointerDown);
    document.addEventListener('keydown', onKeyDown);
    return () => {
      document.removeEventListener('mousedown', onPointerDown);
      document.removeEventListener('keydown', onKeyDown);
    };
  }, [isOpen]);

  return (
    <div ref={containerRef} className="relative">
      <button
        type="button"
        aria-haspopup="menu"
        aria-expanded={isOpen}
        aria-label="Workspace"
        data-testid="workspace-switcher"
        onClick={() => setIsOpen((open) => !open)}
        className="inline-flex max-w-[16rem] items-center gap-2 rounded-sm border border-border px-3 py-2 text-xs font-mono uppercase tracking-wider text-text-primary hover:bg-surface-elevated"
      >
        <span className="truncate">{active?.name ?? 'Workspace'}</span>
        <ChevronDown className="h-4 w-4 shrink-0 text-text-secondary" />
      </button>

      {isOpen && (
        <div
          role="menu"
          className="absolute right-0 z-50 mt-2 w-64 rounded-sm border border-border bg-surface shadow-2xl"
        >
          <div className="max-h-64 overflow-y-auto py-1">
            {workspaces.map((workspace) => (
              <button
                key={workspace.id}
                type="button"
                role="menuitemradio"
                aria-checked={workspace.id === activeWorkspaceId}
                onClick={() => {
                  setIsOpen(false);
                  if (workspace.id !== activeWorkspaceId) onSelect(workspace.id);
                }}
                className={cx(
                  'flex w-full items-center justify-between gap-2 px-4 py-2 text-left text-xs font-mono hover:bg-surface-elevated',
                  workspace.id === activeWorkspaceId ? 'text-primary-green' : 'text-text-primary',
                )}
              >
                <span className="truncate">{workspace.name}</span>
                {workspace.id === activeWorkspaceId && <Check className="h-4 w-4 shrink-0" />}
              </button>
            ))}
          </div>
          <div className="border-t border-border py-1">
            <button
              type="button"
              role="menuitem"
              onClick={() => {
                setIsOpen(false);
                setIsCreating(true);
              }}
              className="flex w-full items-center gap-2 px-4 py-2 text-left text-xs font-mono text-text-secondary hover:bg-surface-elevated hover:text-text-primary"
            >
              <Plus className="h-4 w-4" />
              Crear nuevo workspace
            </button>
          </div>
        </div>
      )}

      {isCreating && <CreateWorkspaceModal onClose={() => setIsCreating(false)} onCreate={onCreate} />}
    </div>
  );
}
