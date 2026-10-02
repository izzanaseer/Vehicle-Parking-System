// Redirect to login if there's no token at all
if (!getToken()) {
  window.location.href = "index.html";
}

// Decode the token's payload to check the role (client-side, just for showing/hiding UI)
function getRoleFromToken(token) {
  const payload = JSON.parse(atob(token.split(".")[1]));
  return payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];
}

const role = getRoleFromToken(getToken());
if (role === "Admin") {
  document.getElementById("adminControls").style.display = "block";
}

document.getElementById("checkAvailability").addEventListener("click", async () => {
  const response = await fetch(`${API_BASE}/parking/availability`, {
    headers: { "Authorization": `Bearer ${getToken()}` }
  });

  if (!response.ok) {
    document.getElementById("availableCount").textContent = "Error";
    return;
  }

  const data = await response.json();
  document.getElementById("availableCount").textContent = data.availableSlots;
  document.getElementById("totalCount").textContent = data.totalSlots;
});

document.getElementById("checkAvailability").click();           //To load availabilty immediately on opening page instead of waiting for click.

document.getElementById("occupySlot").addEventListener("click", async () => {
  const messageEl = document.getElementById("adminMessage");

  const response = await fetch(`${API_BASE}/parking/occupy`, {
    method: "POST",
    headers: { "Authorization": `Bearer ${getToken()}` }
  });

  const data = await response.json();
  messageEl.textContent = response.ok
    ? `Occupied ${data.occupiedSlot}. Available now: ${data.availableSlots}`
    : (data.message || "Failed to occupy a slot.");
});

document.getElementById("releaseSlot").addEventListener("click", async () => {
  const slotNumber = document.getElementById("releaseSlotNumber").value;
  const messageEl = document.getElementById("adminMessage");

  const response = await fetch(`${API_BASE}/parking/release/${slotNumber}`, {
    method: "POST",
    headers: { "Authorization": `Bearer ${getToken()}` }
  });

  const data = await response.json();
  messageEl.textContent = response.ok
    ? `Released ${data.releasedSlot}. Available now: ${data.availableSlots}`
    : (data.message || "Failed to release slot.");
});

document.getElementById("logout").addEventListener("click", () => {
  const confirmed = confirm("Are you sure you want to log out?");
  if (!confirmed) return;

  clearToken();
  window.location.href = "index.html";
});