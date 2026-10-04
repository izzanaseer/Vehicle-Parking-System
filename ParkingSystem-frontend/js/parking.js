if (!getToken()) {
  window.location.href = "index.html";
}

function getRoleFromToken(token) {
  const payload = JSON.parse(atob(token.split(".")[1]));
  return payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];
}

const role = getRoleFromToken(getToken());
if (role === "Admin") {
  document.getElementById("adminControls").style.display = "grid";
}

function getEmailFromToken(token) {
  const payload = JSON.parse(atob(token.split(".")[1]));
  return payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"];
}

document.getElementById("profileEmail").textContent = getEmailFromToken(getToken());
document.getElementById("profileRole").textContent = role;
document.getElementById("welcomeUser").textContent = role;

const vehicleIcons = { Car: "fa-car", Bike: "fa-motorcycle", Truck: "fa-truck" };
const vehicleColors = { Car: "green", Bike: "purple", Truck: "orange" };

document.getElementById("checkAvailability").addEventListener("click", async () => {
  const grid = document.getElementById("statGrid");

  const response = await fetch(`${API_BASE}/parking/availability`, {
    headers: { "Authorization": `Bearer ${getToken()}` }
  });

  if (!response.ok) {
    grid.innerHTML = `<p class="error-text">Failed to load availability.</p>`;
    return;
  }

  const data = await response.json();

  grid.innerHTML = data.map(d => `
  <div class="stat-card stat-${vehicleColors[d.vehicleType] || "green"}">
    <i class="fa-solid ${vehicleIcons[d.vehicleType] || "fa-square-parking"}"></i>
    <div class="stat-numbers">
      <span class="stat-available">${d.available}</span>
      <span class="stat-divider">/</span>
      <span class="stat-total">${d.total}</span>
    </div>
    <p class="stat-label">${d.vehicleType}</p>
  </div>
`).join("");
});

document.getElementById("issueTicket").addEventListener("click", async () => {
  const vehicleNumber = document.getElementById("vehicleNumber").value;
  const vehicleType = document.getElementById("vehicleType").value;
  const resultEl = document.getElementById("issuedTicket");

  const response = await fetch(`${API_BASE}/parking/tickets`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      "Authorization": `Bearer ${getToken()}`
    },
    body: JSON.stringify({ vehicleNumber, vehicleType })
  });

  if (!response.ok) {
    const errorText = await response.text();
    resultEl.innerHTML = `<p class="error-text">${errorText}</p>`;
    return;
  }

  const data = await response.json();

  resultEl.innerHTML = `
    <p class="success-text">Ticket #${data.ticketId} issued — Slot ${data.slotNumber}</p>
    <button id="printBtn" class="secondary-btn"><i class="fa-solid fa-print"></i> Print Ticket</button>
  `;

  document.getElementById("printBtn").addEventListener("click", () => {
    document.getElementById("printTicket").innerHTML = `
      <h2>Parking Ticket</h2>
      <p><strong>Ticket ID:</strong> ${data.ticketId}</p>
      <p><strong>Vehicle:</strong> ${data.vehicleNumber}</p>
      <p><strong>Slot:</strong> ${data.slotNumber}</p>
      <p><strong>Entry Time:</strong> ${new Date(data.entryTime).toLocaleString()}</p>
    `;
    window.print();
  });

  document.getElementById("vehicleNumber").value = "";
  document.getElementById("checkAvailability").click();
});

document.getElementById("calculateExit").addEventListener("click", async () => {
  const ticketId = document.getElementById("exitTicketId").value;
  const resultEl = document.getElementById("exitResult");

  const response = await fetch(`${API_BASE}/parking/tickets/${ticketId}/exit`, {
    method: "PATCH",
    headers: { "Authorization": `Bearer ${getToken()}` }
  });

  if (!response.ok) {
    const errorText = await response.text();
    resultEl.innerHTML = `<p class="error-text">${errorText}</p>`;
    return;
  }

  const data = await response.json();
  resultEl.innerHTML = `<p class="success-text">Fee: Rs. ${data.feeAmount} — ${data.vehicleNumber}</p>`;
  document.getElementById("paymentSection").style.display = "flex";
  document.getElementById("paymentSection").dataset.ticketId = ticketId;
});

document.getElementById("confirmPayment").addEventListener("click", async () => {
  const ticketId = document.getElementById("paymentSection").dataset.ticketId;
  const paymentMethod = document.getElementById("paymentMethod").value;
  const resultEl = document.getElementById("paymentResult");

  const response = await fetch(`${API_BASE}/parking/tickets/${ticketId}/pay`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      "Authorization": `Bearer ${getToken()}`
    },
    body: JSON.stringify({ paymentMethod })
  });

  if (!response.ok) {
    const errorText = await response.text();
    resultEl.innerHTML = `<p class="error-text">${errorText}</p>`;
    return;
  }

  const data = await response.json();
  resultEl.innerHTML = `<p class="success-text">Paid via ${data.paymentMethod}. Slot ${data.slotFreed} freed.</p>`;
  document.getElementById("paymentSection").style.display = "none";
  document.getElementById("exitTicketId").value = "";
  document.getElementById("checkAvailability").click();
});

document.getElementById("logout").addEventListener("click", () => {
  const confirmed = confirm("Are you sure you want to log out?");
  if (!confirmed) return;
  clearToken();
  window.location.href = "index.html";
});

document.getElementById("checkAvailability").click();