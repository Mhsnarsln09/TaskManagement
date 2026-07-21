import { FullPageState } from "@/components/shared/states";

export default function NotFound() {
  return (
    <FullPageState
      code="404"
      title="Sayfa bulunamadı"
      description="Aradığınız kaynak yok ya da erişiminiz kaldırılmış olabilir."
      actionLabel="Projelerime dön"
      actionHref="/projects"
    />
  );
}
