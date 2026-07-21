import { Suspense } from "react";
import { TasksView } from "@/features/tasks/tasks-view";

export default async function TasksPage({
  params,
}: {
  params: Promise<{ projectId: string }>;
}) {
  const { projectId } = await params;
  return (
    // useSearchParams istemci sınırı gerektirir.
    <Suspense>
      <TasksView projectId={projectId} />
    </Suspense>
  );
}
