import { describe, expect, it } from "vitest";

import { canManageTasks, taskPermissions } from "./permissions";
import type { ProjectResponse, TaskResponse, UserResponse } from "@/lib/api/types";

const OWNER_ID = "00000000-0000-0000-0000-00000000owner";
const ADMIN_ID = "00000000-0000-0000-0000-00000000admin";
const ASSIGNEE_ID = "00000000-0000-0000-0000-0000000assign";
const OTHER_ID = "00000000-0000-0000-0000-00000000other";

const project = { ownerUserId: OWNER_ID } as Pick<ProjectResponse, "ownerUserId">;

function user(id: string, roles: string[] = ["Member"]): UserResponse {
  return {
    id,
    email: `${id}@test.local`,
    userName: id,
    displayName: null,
    roles,
  } as UserResponse;
}

const taskAssignedToAssignee = {
  assigneeUserId: ASSIGNEE_ID,
} as Pick<TaskResponse, "assigneeUserId">;

describe("task authority matrix (B10-02)", () => {
  it("owner and Admin can fully manage tasks", () => {
    expect(canManageTasks(user(OWNER_ID), project)).toBe(true);
    expect(canManageTasks(user(ADMIN_ID, ["Admin"]), project)).toBe(true);
  });

  it("a system ProjectManager role alone grants no task authority", () => {
    expect(canManageTasks(user(OTHER_ID, ["ProjectManager", "Member"]), project)).toBe(
      false,
    );
  });

  it("owner: edit all fields, delete, change status", () => {
    const perms = taskPermissions(user(OWNER_ID), project, taskAssignedToAssignee);
    expect(perms).toMatchObject({
      canManage: true,
      canEditAllFields: true,
      canChangeStatus: true,
      canDelete: true,
      readOnly: false,
    });
  });

  it("assignee: status only, no edit, no delete", () => {
    const perms = taskPermissions(user(ASSIGNEE_ID), project, taskAssignedToAssignee);
    expect(perms).toMatchObject({
      canManage: false,
      canEditAllFields: false,
      canChangeStatus: true,
      canDelete: false,
      readOnly: false,
    });
  });

  it("other member: read-only, no writes at all", () => {
    const perms = taskPermissions(user(OTHER_ID), project, taskAssignedToAssignee);
    expect(perms).toMatchObject({
      canManage: false,
      canEditAllFields: false,
      canChangeStatus: false,
      canDelete: false,
      readOnly: true,
    });
  });

  it("Admin who is not the assignee still manages fully", () => {
    const perms = taskPermissions(
      user(ADMIN_ID, ["Admin"]),
      project,
      taskAssignedToAssignee,
    );
    expect(perms.canEditAllFields).toBe(true);
    expect(perms.canDelete).toBe(true);
  });
});
