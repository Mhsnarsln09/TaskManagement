# Claude Design Prompt

Aşağıdaki prompt'u Claude Design'a tek parça halinde ver. Üretilen tasarımlar,
frontend geliştirilirken görsel kaynak olarak kullanılacak.

```text
Design a complete, implementation-ready responsive web application UI for
"TaskManagement", a collaborative project and task management product for small
teams. This is the authenticated product experience, not a marketing landing page.

The frontend will be implemented with Next.js App Router, TypeScript, Tailwind CSS,
shadcn/ui, and Lucide icons. Design with components and interaction patterns that
map naturally to shadcn/ui. Use an 8px maximum card radius, restrained shadows,
clear borders, compact spacing, and accessible focus states.

PRODUCT AND BACKEND CAPABILITIES
- Public registration accepts exactly: email, userName, password, and optional
  displayName. NEVER show a role selector during registration. The backend always
  assigns the Member role. Registration must contain separate username and email
  fields; displayName is optional. Password requires at least 8 characters with an
  uppercase letter, a lowercase letter, and a digit.
- Login accepts one userNameOrEmail field and password.
- Auth responses contain the current user's roles: Admin, ProjectManager, Member.
  Roles affect permissions after login but users cannot select, request, edit, or
  assign roles anywhere in the current frontend.
- There is currently NO SuperAdmin, user administration, role management, user
  directory, or user search API. Do not design any screen or control for them.
- Users only see projects they own or belong to.
- Projects can be created, viewed, edited, and deleted.
- Project owners can list, add, and remove project members. The current member list
  API returns only userId and joinedAtUtc; it does not return name, email, role, or
  avatar. Adding a member accepts only userId. Show IDs honestly and do not invent a
  user picker, search results, profile information, role selector, or avatar.
- Each project has statistics: total, todo, in progress, completed, cancelled,
  overdue, and completion percentage.
- Tasks belong to a project and include title, description, status, priority, due
  date, assigneeUserId, overdue state, created time, and updated time. Task responses
  do not contain assignee names or avatars. Assignment accepts a project member's
  GUID; do not invent a people search or profile display.
- Task status values are Todo, InProgress, Completed, and Cancelled. Priority values
  are Low, Medium, High, and Critical.
- Tasks support paginated lists, one status filter, one priority filter, and sorting
  by title, status, priority, dueDate, or createdAtUtc in asc/desc direction. There is
  no task text-search endpoint.
- Task details contain paginated comments and file attachments. Users can add a
  comment, upload an attachment, and download attachments.
- Projects return id, name, optional description, ownerUserId, createdAtUtc, and
  optional updatedAtUtc. Do not invent project icons, team names, recent activity,
  customer data, or task counts on the project list.
- Users receive paginated notifications. New notifications arrive in real time via
  SignalR and can be marked as read.
- The backend returns validation errors and 400, 401, 403, 404, 409, 429 states.
- Do not invent unsupported features such as chat, mentions, tags, subtasks,
  calendar, Gantt, time tracking, user avatars/photos, drag-and-drop Kanban, role
  selection, role administration, user search, global search, or activity feeds.

API-CONTRACT RULE (MANDATORY)
Every visible value, form field, filter, action, and table column must map to a field
or operation explicitly listed above. If an attractive interaction requires missing
backend data, omit it instead of mocking it. Never label unsupported controls as
"coming soon" and never include speculative controls in the primary UI.

AUDIENCE AND VISUAL DIRECTION
Create a quiet, professional, work-focused operational interface. It should feel
modern, precise, and trustworthy rather than playful or marketing-oriented. Avoid
giant headings, decorative gradients, purple/blue-dominated palettes, beige themes,
floating page-section cards, nested cards, glassmorphism, illustration-heavy empty
states, and excessive pills. Use a balanced neutral base with a restrained teal or
green product accent, semantic red/amber/green/blue status colors, and excellent
contrast. Do not rely on color alone: always pair status color with text or icon.
Use Inter or a similarly readable UI sans-serif with normal letter spacing.

INFORMATION ARCHITECTURE
Desktop: fixed left navigation with product name, project switcher, Overview, Tasks,
Members, and a lower user/account area. Use a compact top bar for breadcrumbs,
contextual actions, notifications, and the user menu. Mobile: replace the sidebar
with a compact header and accessible navigation drawer; preserve every core action.
Do not place the main application inside a decorative outer card.

DESIGN THESE SCREENS AND STATES
1. Login and registration screens with inline validation, password visibility,
   submitting, invalid credentials, and session-expired states. Registration fields
   are exactly Display name (optional), Username, Email, and Password. It has no role
   field, company field, password confirmation, terms checkbox, or social login.
2. Project list with useful metadata, create-project dialog, empty state, loading
   skeleton, API error, and no-access state.
3. Project overview with literal project name as the page title, concise description,
   completion progress, six task metrics, and an actionable overdue section. Do not
   add an activity feed because no activity endpoint exists.
4. Task list as a dense, scannable table on desktop and structured rows on mobile.
   Include implemented status/priority filters, sorting, pagination, overdue state,
   assigneeUserId, due date, clear-filters action, create-task action, loading, empty,
   filtered-empty, error, and 429 rate-limit states.
5. Create/edit task form using appropriate inputs: text input, textarea, select or
   combobox, date picker, and clear validation. Include unsaved-change behavior,
   submitting state, and conflict (409) resolution message.
6. Task detail as a focused page or responsive side sheet. Show metadata first,
   editable actions based on permission, then comments and attachments. Include
   paginated comments, add-comment composer, upload progress, file size/type,
   download action, upload validation, and no-attachment state.
7. Members screen as a compact table/list with user ID and joined date only. The
   project owner can be identified by matching userId with ownerUserId. Add-member
   uses a GUID field. Include remove confirmation, duplicate-member conflict, and
   permission states. Never show member names, email, avatars, or roles.
8. Notification center as a popover plus a full responsive view when needed. Show
   read/unread distinction, timestamp, mark-as-read action, pagination/loading, empty
   state, real-time arrival toast, reconnecting indicator, and refresh after reconnect.
9. Project settings/edit/delete flow with a destructive confirmation that requires
   clear acknowledgement.
10. Global 403, 404, generic error, offline/reconnecting, and session-expired states.

INTERACTION AND COMPONENT RULES
- Use Lucide icons for familiar actions. Icon-only controls require tooltips and
  accessible labels.
- Use buttons for commands, tabs for views, selects/menus for option sets, badges
  only for concise statuses, progress for completion, dialogs for short creation or
  confirmation flows, and sheets only where they improve responsive task detail.
- Keep controls stable in size; text must never overlap or overflow.
- Show destructive actions in menus or clear danger zones, never as dominant default
  actions. Require confirmation for deleting a project/task or removing a member.
- Show permission-aware states: hide impossible actions and explain disabled actions
  when context matters. A 404 may intentionally represent an inaccessible resource.
- Dates should be designed for localized display while data is stored as UTC/ISO.
- Meet WCAG AA contrast, keyboard navigation, visible focus, 44px mobile targets,
  semantic labels, and screen-reader-friendly validation.

COMPONENT-FIRST ARCHITECTURE (MANDATORY)
Design and, when generating frontend code, build reusable components before composing
pages. Do not duplicate page-specific versions of the same control. Organize the
handoff and implementation in this order:
1. Design tokens: colors, typography, spacing, radius, borders, shadows, breakpoints.
2. shadcn-based primitives: Button, Input, Textarea, Label, Select, Checkbox, Badge,
   Tooltip, Dialog, AlertDialog, Sheet, DropdownMenu, Table, Tabs, Progress, Skeleton,
   Toast, Pagination, and FormField. Extend through variants rather than copy/paste.
3. Shared composites: AppShell, SidebarNav, MobileNav, PageHeader, Breadcrumbs,
   DataTable, FilterBar, EmptyState, ErrorState, LoadingState, ConfirmDialog,
   ProblemDetailsAlert, StatusBadge, PriorityBadge, UserIdDisplay, DateDisplay,
   NotificationItem, and FileAttachmentItem.
4. Feature components: auth forms, project form/list/summary, task form/list/detail,
   member list/form, comments, attachments, and notification center.
5. Route pages that compose those components and contain minimal presentation logic.

Define each shared component once with its props, variants, responsive behavior,
loading/disabled/error states, and accessibility contract. Screens must reference
these shared components by the same names. Keep API calls and business state outside
presentational UI components. Use composition and feature boundaries; do not create
one giant dashboard component.

DELIVERABLE
First produce the design tokens and reusable component library, then produce all
screens in desktop and mobile variants by composing that library. Include a component
inventory and a screen-to-component mapping. Include realistic Turkish sample content
that obeys the exact API fields, plus annotations for interaction, responsive
behavior, loading, empty, error, validation, permission, and destructive states.
Keep the UI feasible to reproduce faithfully with Tailwind CSS and shadcn/ui.
Prioritize implementation clarity, reuse, and workflow completeness over spectacle.
```
