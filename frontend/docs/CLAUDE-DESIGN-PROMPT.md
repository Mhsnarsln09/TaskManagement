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
- Users register, log in, log out, and have roles: Admin, ProjectManager, Member.
- Users only see projects they own or belong to.
- Projects can be created, viewed, edited, and deleted.
- Project owners can list, add, and remove project members.
- Each project has statistics: total, todo, in progress, completed, cancelled,
  overdue, and completion percentage.
- Tasks belong to a project and include title, description, status, priority, due
  date, assignee, overdue state, created time, and updated time.
- Task status values are Todo, InProgress, Completed, and Cancelled. Priority values
  are Low, Medium, High, and Critical.
- Tasks support paginated lists, status and priority filters, and sorting.
- Task details contain paginated comments and file attachments. Users can add a
  comment, upload an attachment, and download attachments.
- Users receive paginated notifications. New notifications arrive in real time via
  SignalR and can be marked as read.
- The backend returns validation errors and 400, 401, 403, 404, 409, 429 states.
- Do not invent unsupported features such as chat, mentions, tags, subtasks,
  calendar, Gantt, time tracking, user avatars/photos, or drag-and-drop Kanban.

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
   submitting, invalid credentials, and session-expired states.
2. Project list with useful metadata, create-project dialog, empty state, loading
   skeleton, API error, and no-access state.
3. Project overview with literal project name as the page title, concise description,
   completion progress, six task metrics, an actionable overdue section, and recent
   task activity based only on available task data.
4. Task list as a dense, scannable table on desktop and structured rows on mobile.
   Include search only as a visual affordance marked "requires backend support";
   include implemented status/priority filters, sorting, pagination, overdue state,
   assignee, due date, clear-filters action, create-task action, loading, empty,
   filtered-empty, error, and 429 rate-limit states.
5. Create/edit task form using appropriate inputs: text input, textarea, select or
   combobox, date picker, and clear validation. Include unsaved-change behavior,
   submitting state, and conflict (409) resolution message.
6. Task detail as a focused page or responsive side sheet. Show metadata first,
   editable actions based on permission, then comments and attachments. Include
   paginated comments, add-comment composer, upload progress, file size/type,
   download action, upload validation, and no-attachment state.
7. Members screen as a compact table/list with owner distinction, joined date,
   add-member dialog using a user ID field because the backend has no user search
   endpoint, remove confirmation, duplicate-member conflict, and permission states.
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

DELIVERABLE
Produce a cohesive design system and all screens above in desktop and mobile
variants. Include color/type/spacing tokens, component variants, realistic Turkish
sample content, and annotations for interaction, responsive behavior, loading,
empty, error, validation, permission, and destructive states. Keep the UI feasible
to reproduce faithfully with Tailwind CSS and shadcn/ui. Prioritize implementation
clarity and workflow completeness over visual spectacle.
```
