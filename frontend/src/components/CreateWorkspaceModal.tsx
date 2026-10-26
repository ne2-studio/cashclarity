import { Modal } from '../design-system';
import { CreateWorkspaceForm } from './CreateWorkspaceForm';

interface CreateWorkspaceModalProps {
  onClose: () => void;
  onCreate: (name: string) => Promise<void>;
}

export function CreateWorkspaceModal({ onClose, onCreate }: CreateWorkspaceModalProps) {
  return (
    <Modal title="Crear nuevo workspace" subtitle="Cada workspace tiene su propio extracto bancario y contabilidad" onClose={onClose}>
      <CreateWorkspaceForm
        onCancel={onClose}
        onSubmit={async (name) => {
          await onCreate(name);
          onClose();
        }}
      />
    </Modal>
  );
}
