import type { Metadata } from "next";
import { AdminUsers } from "@/features/admin/admin-users";

export const metadata: Metadata = { title: "Kullanıcı yönetimi" };

export default function AdminUsersPage() {
  return <AdminUsers />;
}
