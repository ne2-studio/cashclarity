import type { Meta, StoryObj } from '@storybook/react-vite';
import { expect, userEvent, within } from 'storybook/test';
import { WorkspaceSwitcher } from './WorkspaceSwitcher';

const workspaces = [
  { id: 'ws-1', name: 'Cuenta Sabadell', createdAt: '2026-08-01T00:00:00Z' },
  { id: 'ws-2', name: 'Cuenta BBVA', createdAt: '2026-09-01T00:00:00Z' },
];

const meta = {
  title: 'Components/WorkspaceSwitcher',
  component: WorkspaceSwitcher,
  args: {
    workspaces,
    activeWorkspaceId: 'ws-1',
    onSelect: () => undefined,
    onCreate: async () => undefined,
  },
  decorators: [(Story) => <div className="flex min-h-72 justify-end p-4"><Story /></div>],
} satisfies Meta<typeof WorkspaceSwitcher>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Closed: Story = {};

export const OpenWithOptions: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await userEvent.click(canvas.getByRole('button', { name: 'Workspace' }));
    await expect(canvas.getByRole('menuitem', { name: 'Crear nuevo workspace' })).toBeVisible();
    await expect(canvas.getAllByRole('menuitemradio')).toHaveLength(2);
  },
};

export const CreateModal: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await userEvent.click(canvas.getByRole('button', { name: 'Workspace' }));
    await userEvent.click(canvas.getByRole('menuitem', { name: 'Crear nuevo workspace' }));
    await expect(canvas.getByRole('heading', { name: 'Crear nuevo workspace' })).toBeVisible();
    await expect(canvas.getByLabelText('Nombre del workspace')).toBeVisible();
  },
};
