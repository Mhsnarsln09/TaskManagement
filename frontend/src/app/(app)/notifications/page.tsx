import type { Metadata } from "next";
import { NotificationsPage } from "@/features/notifications/notifications-page";

export const metadata: Metadata = { title: "Bildirimler" };

export default function Page() {
  return <NotificationsPage />;
}
