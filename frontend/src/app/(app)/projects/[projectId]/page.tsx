import { ProjectOverview } from "@/features/projects/project-overview";

// Next 16: params asenkron erişilir.
export default async function ProjectOverviewPage({
  params,
}: {
  params: Promise<{ projectId: string }>;
}) {
  const { projectId } = await params;
  return <ProjectOverview projectId={projectId} />;
}
