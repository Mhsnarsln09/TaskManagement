import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi, beforeEach } from "vitest";

const replaceMock = vi.fn();
vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace: replaceMock, push: vi.fn() }),
  usePathname: () => "/register",
  useSearchParams: () => new URLSearchParams(),
}));

import { RegisterForm } from "./register-form";
import { AuthProvider } from "@/lib/auth/auth-context";

function renderForm() {
  return render(
    <AuthProvider>
      <RegisterForm />
    </AuthProvider>,
  );
}

describe("RegisterForm", () => {
  beforeEach(() => {
    replaceMock.mockReset();
  });

  it("sözleşmedeki alanları gösterir; rol seçici yoktur", () => {
    renderForm();
    expect(screen.getByLabelText(/görünen ad/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/kullanıcı adı/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/e-posta/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/^parola$/i)).toBeInTheDocument();
    // Rol seçici yok: hiçbir combobox/radio ve rol adı render edilmez.
    expect(screen.queryByRole("combobox")).not.toBeInTheDocument();
    expect(screen.queryByText(/superadmin|projectmanager|member/i)).not.toBeInTheDocument();
  });

  it("parola kurallarını satır içi doğrular", async () => {
    const user = userEvent.setup();
    renderForm();

    await user.type(screen.getByLabelText(/kullanıcı adı/i), "aysedemir");
    await user.type(screen.getByLabelText(/e-posta/i), "ayse@ornek.com");
    await user.type(screen.getByLabelText(/^parola$/i), "kucukharf1");
    await user.click(screen.getByRole("button", { name: /hesap oluştur/i }));

    expect(
      await screen.findByText("En az bir büyük harf içermelidir."),
    ).toBeInTheDocument();
  });

  it("boşluklu kullanıcı adını reddeder", async () => {
    const user = userEvent.setup();
    renderForm();

    await user.type(screen.getByLabelText(/kullanıcı adı/i), "ayse demir");
    await user.type(screen.getByLabelText(/e-posta/i), "ayse@ornek.com");
    await user.type(screen.getByLabelText(/^parola$/i), "Parola123");
    await user.click(screen.getByRole("button", { name: /hesap oluştur/i }));

    expect(
      await screen.findByText("Kullanıcı adı boşluk içeremez."),
    ).toBeInTheDocument();
  });

  it("başarılı kayıtta tokenları yazar ve projelere yönlendirir", async () => {
    const user = userEvent.setup();
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValue(
      new Response(
        JSON.stringify({
          accessToken: "a",
          expiresAtUtc: new Date().toISOString(),
          refreshToken: "r",
          refreshTokenExpiresAtUtc: new Date().toISOString(),
          user: {
            id: "11111111-1111-1111-1111-111111111111",
            email: "ayse@ornek.com",
            userName: "aysedemir",
            displayName: null,
            roles: ["Member"],
          },
        }),
        { status: 200, headers: { "Content-Type": "application/json" } },
      ),
    );

    renderForm();
    await user.type(screen.getByLabelText(/kullanıcı adı/i), "aysedemir");
    await user.type(screen.getByLabelText(/e-posta/i), "ayse@ornek.com");
    await user.type(screen.getByLabelText(/^parola$/i), "Parola123");
    await user.click(screen.getByRole("button", { name: /hesap oluştur/i }));

    await waitFor(() => expect(replaceMock).toHaveBeenCalledWith("/projects"));
    const [url, init] = fetchMock.mock.calls[0]!;
    expect(String(url)).toContain("/api/auth/register");
    const body = JSON.parse(String(init?.body));
    expect(body).toEqual({
      displayName: null,
      userName: "aysedemir",
      email: "ayse@ornek.com",
      password: "Parola123",
    });
    expect(window.localStorage.getItem("taskmanagement.session")).toContain(
      "aysedemir",
    );
  });
});
