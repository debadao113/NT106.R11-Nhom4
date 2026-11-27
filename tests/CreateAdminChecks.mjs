import assert from "node:assert/strict";
import { createHandler } from "../supabase/functions/create-admin/handler.ts";

const callerEmail = "debadao113@gmail.com";
const requestId = "11111111-1111-4111-8111-111111111111";
const payload = { UsernameOrEmail: "Long", DisplayName: "Long", Password: "fixture-password-only", RequestId: requestId, Role: "super_admin" };
const json = (data, status = 200) => new Response(JSON.stringify(data), { status });

async function scenario(options = {}) {
  const calls = [];
  let saved;
  const handler = createHandler({ projectUrl: "https://fixture.supabase.co", serviceKey: "server-only-test-key",
    fetchImpl: async (url, init) => {
      const path = new URL(url).pathname;
      calls.push({ path, method: init.method, body: init.body && JSON.parse(init.body) });
      if (path === "/auth/v1/user") {
        assert.equal(init.headers.Authorization, "Bearer verified-session");
        return options.invalidToken ? json({}, 401) : json({ id: "caller-id", email: options.email ?? callerEmail });
      }
      assert.equal(init.headers.Authorization, "Bearer server-only-test-key");
      if (url.includes("user_id=eq.caller-id")) return json([{ role: options.role ?? "admin", is_active: options.active ?? true }]);
      if (url.includes("creation_request_id=")) return json(options.previous ? [options.previous] : []);
      if (path === "/auth/v1/admin/users" && init.method === "POST") {
        assert.equal(JSON.parse(init.body).password, payload.Password);
        return options.duplicate ? json({ code: "email_exists" }, 422) : json({ id: "new-user-id" });
      }
      if (path === "/rest/v1/profiles" && init.method === "POST") {
        saved = JSON.parse(init.body);
        assert.equal(saved.role, "admin", "Created users must always be ordinary admins");
        assert.equal(saved.email, "long@staff.hoteldesk.invalid");
        if (options.lostResponse) throw new Error("Network lost after commit");
        return options.profileFailure ? json({}, 500) : json([saved], 201);
      }
      if (url.includes("user_id=eq.new-user-id")) return json(options.lostResponse ? [saved] : []);
      if (path === "/auth/v1/admin/users/new-user-id" && init.method === "DELETE") return json({});
      throw new Error("Unexpected fixture request: " + url);
    },
  });
  const response = await handler(new Request("https://fixture/functions/v1/create-admin", {
    method: "POST", headers: options.noToken ? {} : { Authorization: "Bearer verified-session" },
    body: JSON.stringify(options.body ?? payload),
  }));
  const result = await response.json();
  assert.ok(!JSON.stringify(result).includes(payload.Password));
  assert.ok(!JSON.stringify(result).includes("server-only-test-key"));
  return { status: response.status, result, calls };
}

for (const options of [ { noToken: true }, { invalidToken: true } ]) {
  const result = await scenario(options);
  assert.equal(result.status, 401);
  assert.ok(!result.calls.some(c => c.method === "POST"));
}
for (const options of [ { role: "receptionist" }, { role: "unknown" }, { active: false } ]) {
  const result = await scenario(options);
  assert.equal(result.status, 403);
  assert.ok(!result.calls.some(c => c.method === "POST"));
}
const created = await scenario();
assert.equal(created.status, 201);
assert.equal(created.result.Role, "admin");
assert.equal(created.result.UserId, "new-user-id");
assert.equal((await scenario({ duplicate: true })).status, 409);
assert.equal((await scenario({ body: { ...payload, Password: "123" } })).status, 400);
assert.equal((await scenario({ email: "another-admin@example.com" })).status, 201);
const failed = await scenario({ profileFailure: true });
assert.equal(failed.status, 503);
assert.equal(failed.calls.filter(c => c.method === "DELETE").length, 1);
assert.equal(failed.calls.find(c => c.method === "DELETE").path, "/auth/v1/admin/users/new-user-id");
const reconciled = await scenario({ lostResponse: true });
assert.equal(reconciled.status, 200);
assert.ok(!reconciled.calls.some(c => c.method === "DELETE"));
const repeated = await scenario({ previous: { user_id: "new-user-id", email: "long@staff.hoteldesk.invalid", display_name: "Long", role: "admin", is_active: true } });
assert.equal(repeated.status, 200);
assert.ok(!repeated.calls.some(c => c.method === "POST"));
console.log("PASS: All active admins can create admins; receptionist/disabled denial, fixed role, validation and reconciliation.");
