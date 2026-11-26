// No service key or user password is ever returned to the WinForms app.
type Options = { projectUrl: string; serviceKey: string; fetchImpl?: typeof fetch };
type Profile = { user_id: string; email: string; display_name: string; role: string; is_active: boolean; creation_request_id?: string };

class ApiError extends Error {
  status: number;
  constructor(status: number, message: string) { super(message); this.status = status; }
}

export function createHandler(options: Options) {
  const requestFetch = options.fetchImpl ?? fetch;
  const origin = options.projectUrl.replace(/\/$/, "");

  async function api(path: string, method = "GET", body?: unknown, token = options.serviceKey) {
    const response = await requestFetch(origin + path, {
      method,
      headers: { apikey: options.serviceKey, Authorization: `Bearer ${token}`,
        "Content-Type": "application/json", Prefer: "return=representation" },
      body: body === undefined ? undefined : JSON.stringify(body),
      signal: AbortSignal.timeout(15000),
    });
    const text = await response.text();
    let data;
    try { data = text ? JSON.parse(text) : null; } catch { data = null; }
    return { ok: response.ok, status: response.status, data };
  }

  const asAccount = (p: Profile) => ({ UserId: p.user_id, Email: p.email, DisplayName: p.display_name, Role: p.role });
  const json = (status: number, data: unknown) => new Response(JSON.stringify(data), {
    status, headers: { "Content-Type": "application/json", "Cache-Control": "no-store" },
  });

  return async (request: Request): Promise<Response> => {
    try {
      if (request.method !== "POST") return json(405, { message: "Chỉ hỗ trợ POST." });
      if (!options.projectUrl || !options.serviceKey) throw new ApiError(503, "Chức năng tạo tài khoản chưa được cấu hình trên Supabase.");
      const authorization = request.headers.get("Authorization") ?? "";
      if (!/^Bearer [^\s]+$/i.test(authorization)) throw new ApiError(401, "Vui lòng đăng nhập lại.");

      // Verify the caller with Auth. Never trust a decoded but unverified JWT,
      // caller-supplied role, or user_metadata.
      const user = await api("/auth/v1/user", "GET", undefined, authorization.slice(7));
      if (!user.ok || !user.data?.id) throw new ApiError(401, "Phiên đăng nhập không hợp lệ.");
      const caller = await api(`/rest/v1/profiles?user_id=eq.${encodeURIComponent(user.data.id)}&select=user_id,role,is_active`);
      if (!caller.ok) throw new ApiError(503, "Không thể kiểm tra quyền. Vui lòng thử lại sau.");
      if (caller.data?.length !== 1 || caller.data[0].role !== "admin" || !caller.data[0].is_active)
        throw new ApiError(403, "Chỉ admin đang hoạt động được tạo tài khoản admin.");

      const raw = await request.text();
      if (raw.length > 8192) throw new ApiError(400, "Dữ liệu quá dài.");
      let body;
      try { body = JSON.parse(raw); } catch { throw new ApiError(400, "Dữ liệu không hợp lệ."); }
      if (body === null || typeof body !== "object") throw new ApiError(400, "Dữ liệu không hợp lệ.");
      const identity = typeof body.UsernameOrEmail === "string" ? body.UsernameOrEmail.trim() : "";
      const displayName = typeof body.DisplayName === "string" ? body.DisplayName.trim() : "";
      const password = typeof body.Password === "string" ? body.Password : "";
      const requestId = typeof body.RequestId === "string" ? body.RequestId : "";
      if (!/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(requestId))
        throw new ApiError(400, "Mã yêu cầu không hợp lệ.");
      let email: string;
      if (identity.includes("@")) {
        if (identity.length > 254 || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(identity)) throw new ApiError(400, "Email không hợp lệ.");
        email = identity.toLowerCase();
      } else {
        if (!/^[A-Za-z0-9][A-Za-z0-9._-]{0,49}$/.test(identity)) throw new ApiError(400, "Tên tài khoản không hợp lệ.");
        email = identity.toLowerCase() + "@staff.hoteldesk.invalid";
      }
      if (!displayName || displayName.length > 100) throw new ApiError(400, "Tên hiển thị cần từ 1 đến 100 ký tự.");
      if (password.length < 6 || password.length > 128) throw new ApiError(400, "Mật khẩu cần từ 6 đến 128 ký tự và đáp ứng chính sách project.");

      const previous = await api(`/rest/v1/profiles?creation_request_id=eq.${requestId}&select=*`);
      if (!previous.ok) throw new ApiError(503, "Chưa triển khai migration quản trị tài khoản hoặc Supabase chưa sẵn sàng.");
      if (previous.data?.length) {
        const p = previous.data[0] as Profile;
        if (p.email !== email || p.role !== "admin" || !p.is_active) throw new ApiError(409, "Mã yêu cầu đã được sử dụng.");
        return json(200, asAccount(p));
      }

      // Auth account creation also enforces the actual project password policy.
      const created = await api("/auth/v1/admin/users", "POST", {
        email, password, email_confirm: true, user_metadata: { display_name: displayName },
      });
      if (!created.ok || !created.data?.id) {
        const code = created.data?.code ?? created.data?.error_code;
        if (code === "email_exists" || code === "user_already_exists") throw new ApiError(409, "Tài khoản đã tồn tại. Hãy tải lại danh sách.");
        if (code === "weak_password") throw new ApiError(400, "Mật khẩu chưa đáp ứng chính sách Supabase.");
        if (created.status >= 500) throw new ApiError(503, "Chưa xác định được kết quả tạo tài khoản. Kiểm tra danh sách trước khi tạo lại.");
        throw new ApiError(400, "Không thể tạo tài khoản. Kiểm tra tên, email và chính sách mật khẩu.");
      }

      const profile: Profile = { user_id: created.data.id, email, display_name: displayName,
        role: "admin", is_active: true, creation_request_id: requestId };
      try {
        const saved = await api("/rest/v1/profiles", "POST", profile);
        if (!saved.ok) throw new Error("profile write failed");
      } catch {
        // Auth and PostgREST do not share a transaction. Reconcile a lost write
        // response before compensating; only the user created above is touched.
        const check = await api(`/rest/v1/profiles?user_id=eq.${encodeURIComponent(profile.user_id)}&select=*`).catch(() => null);
        if (check?.ok && check.data?.[0]?.creation_request_id === requestId
          && check.data[0].role === "admin" && check.data[0].is_active) return json(200, asAccount(profile));
        const removed = await api(`/auth/v1/admin/users/${encodeURIComponent(profile.user_id)}`, "DELETE").catch(() => null);
        if (!removed?.ok) throw new ApiError(503, "Tài khoản Auth có thể đã tạo nhưng chưa cấp được quyền. Kiểm tra Supabase Auth trước khi tạo lại.");
        throw new ApiError(503, "Chưa cấp được quyền; đã hoàn tác tài khoản vừa tạo. Vui lòng thử lại sau.");
      }
      return json(201, asAccount(profile));
    } catch (error) {
      if (error instanceof ApiError) return json(error.status, { message: error.message });
      return json(503, { message: "Chưa xác định được kết quả. Hãy kiểm tra danh sách tài khoản trước khi tạo lại." });
    }
  };
}
