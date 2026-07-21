import { ProjectMembers } from "@/features/projects/project-members";

export default async function ProjectMembersPage({
  params,
}: {
  params: Promise<{ projectId: string }>;
}) {
  const { projectId } = await params;
  return <ProjectMembers projectId={projectId} />;
}
