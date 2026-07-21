"use client";

import { useQuery } from "@tanstack/react-query";
import { useEffect } from "react";
import { projectsApi } from "@/lib/api/endpoints";
import { userDirectory } from "@/lib/user-directory";

/**
 * Proje üyelerini çeker ve yanıttaki kullanıcı özetlerini dizine yazar.
 * Üyeler birden çok ekranda kullanıldığı için (üye tablosu, görev atama seçici)
 * dizin beslemesi tek yerde yapılır; aksi halde bir ekran adları gösterirken
 * diğeri GUID gösterir.
 */
export function useProjectMembers(projectId: string, enabled = true) {
  const query = useQuery({
    queryKey: ["project", projectId, "members"],
    queryFn: ({ signal }) => projectsApi.members(projectId, signal),
    enabled,
  });

  useEffect(() => {
    const summaries = (query.data ?? [])
      .map((member) => member.user)
      .filter((summary) => summary !== null);
    if (summaries.length > 0) userDirectory.upsertMany(summaries);
  }, [query.data]);

  return query;
}
