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

// Availability — per vehicle type
document.getElementById("checkAvailability").addEventListener("click", async () => {
  const response = await fetch(`${API_BASE}/parking/availability`, {
    headers: { "Authorization": `Bearer ${getToken()}` }
  });

  if (!response.ok) {
    document.getElementById("availableCount").textContent = "Error";
    return;
  }

  const data = await response.json();
  const car = data.find(d => d.vehicleType === "Car");
  document.getElementById("availableCount").textContent = car ? car.available : "-";
  document.getElementById("totalCount").textContent = car ? car.total : "-";
});

document.getElementById("checkAvailability").click();           //To load availabilty immediately on opening page instead of waiting for click.

// Issue ticket
document.getElementById("issueTicket").addEventListener("click", async () => {
  const vehicleNumber = document.getElementById("vehicleNumber").value;
  const vehicleType = document.getElementById("vehicleType").value;
  const resultEl = document.getElementById("issuedTicket");

  const response = await fetch(`${API_BASE}/parking/tickets`, {
    method: "POST",
    headers: 
    {
      "Content-Type": "application/json",
      "Authorization": `Bearer ${getToken()}`
    },
    body: JSON.stringify({ vehicleNumber, vehicleType })
  });

  if (!response.ok) 
  {
    const errorText = await response.text();
    resultEl.innerHTML = `<p class="error-text">${errorText}</p>`;
    return;
  }

  const data = await response.json();

  resultEl.innerHTML = `
    <p>Ticket #${data.ticketId} — Slot ${data.slotNumber} — ${data.vehicleNumber}</p>
    <button id="printBtn" class="secondary-btn">Print Ticket</button>
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

// Calculate exit
document.getElementById("calculateExit").addEventListener("click", async () => {
  const ticketId = document.getElementById("exitTicketId").value;
  const resultEl = document.getElementById("exitResult");

  const response = await fetch(`${API_BASE}/parking/tickets/${ticketId}/exit`, {
    method: "PATCH",
    headers: { "Authorization": `Bearer ${getToken()}` }
  });

  if (!response.ok) 
  {
    const errorText = await response.text();
    resultEl.textContent = errorText;
    return;
  }

  const data = await response.json();
  resultEl.textContent = `Fee: Rs. ${data.feeAmount} for ${data.vehicleNumber}`;
  document.getElementById("paymentSection").style.display = "flex";
  document.getElementById("paymentSection").dataset.ticketId = ticketId;
});

// Confirm payment
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
    resultEl.textContent = errorText;
    return;
  }

  const data = await response.json();
  resultEl.textContent = `Paid via ${data.paymentMethod}. Slot ${data.slotFreed} freed.`;
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