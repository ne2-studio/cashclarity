interface SectionHeaderProps {
  title: string;
}

export function SectionHeader({ title }: SectionHeaderProps) {
  return (
    <div className="flex items-center gap-4 mb-6">
      <h2 className="text-[10px] font-mono font-bold uppercase tracking-[0.2em] text-text-secondary whitespace-nowrap">
        {title}
      </h2>
      <div className="h-px w-full bg-border" />
    </div>
  );
}
