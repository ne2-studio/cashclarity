import type { Meta, StoryObj } from '@storybook/react-vite';
import { Button } from './Button';
import { Modal } from './Modal';

const meta = {
  title: 'Design System/Modal',
  component: Modal,
} satisfies Meta<typeof Modal>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {
    title: 'Identificar Entidad',
    subtitle: 'Movimiento: Transferencia cliente norte',
    onClose: () => undefined,
    children: <p className="text-sm text-text-secondary">Contenido del modal.</p>,
    footer: (
      <>
        <Button variant="ghost">Cancelar</Button>
        <Button>Guardar</Button>
      </>
    ),
  },
};
