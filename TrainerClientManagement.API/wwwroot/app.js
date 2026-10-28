const state = {
    trainers: [],
    clients: [],
    appointments: [],
    view: "dashboard",
    editing: null
};

const viewInfo = {
    dashboard: ["Übersicht", "Die heutige Übersicht Ihres Studios."],
    appointments: ["Termine", "Verwalten Sie Ihren Trainingskalender."],
    clients: ["Kunden", "Verwalten Sie Kundendaten und Trainerzuweisungen."],
    trainers: ["Trainer", "Verwalten Sie Ihr Trainerteam."]
};

const dialogTitles = {
    trainer: { create: "Neuen Trainer erstellen", edit: "Trainer bearbeiten" },
    client: { create: "Neuen Kunden erstellen", edit: "Kunden bearbeiten" },
    appointment: { create: "Neuen Termin erstellen", edit: "Termin bearbeiten" }
};

const labels = {
    Scheduled: "Geplant",
    Completed: "Abgeschlossen",
    Cancelled: "Abgesagt"
};

const $ = (selector) => document.querySelector(selector);
const escapeHtml = (value) => String(value ?? "").replace(/[&<>"']/g, (character) => ({
    "&": "&amp;",
    "<": "&lt;",
    ">": "&gt;",
    '"': "&quot;",
    "'": "&#39;"
}[character]));

async function api(path, options = {}) {
    const response = await fetch(path, {
        ...options,
        headers: {
            ...(options.body ? { "Content-Type": "application/json" } : {}),
            ...options.headers
        }
    });

    if (response.status === 204) return null;
    const contentType = response.headers.get("content-type") || "";
    const result = contentType.includes("json") ? await response.json() : null;
    if (!response.ok) {
        const validationErrors = result?.errors
            ? Object.values(result.errors).flat().join(" ")
            : "";
        throw new Error(validationErrors || result?.detail || result?.title || `Die Anfrage ist fehlgeschlagen (${response.status}).`);
    }
    return result;
}

function showNotice(message, type = "success") {
    const notice = $("#notice");
    notice.textContent = message;
    notice.className = `notice ${type}`;
    notice.hidden = false;
    window.clearTimeout(showNotice.timeout);
    showNotice.timeout = window.setTimeout(() => { notice.hidden = true; }, 5000);
}

function initials(name) {
    return String(name || "?").trim().split(/\s+/).slice(0, 2).map((part) => part[0] || "").join("").toLocaleUpperCase("de-DE");
}

function person(name) {
    return `<span class="person-cell"><span class="person-avatar">${escapeHtml(initials(name))}</span>${escapeHtml(name)}</span>`;
}

function formatDate(value) {
    if (!value) return "—";
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return "—";
    return new Intl.DateTimeFormat("de-DE", { dateStyle: "medium", timeStyle: "short" }).format(date);
}

function statusBadge(status) {
    const key = ["Scheduled", "Completed", "Cancelled"].includes(status) ? status : "Scheduled";
    return `<span class="status status-${key.toLowerCase()}">${labels[key]}</span>`;
}

function emptyRow(columns, message) {
    return `<tr><td class="empty-cell" colspan="${columns}">${escapeHtml(message)}</td></tr>`;
}

async function loadData() {
    try {
        const [trainers, clients, appointments] = await Promise.all([
            api("/api/trainers"),
            api("/api/clients"),
            api("/api/appointments")
        ]);
        state.trainers = trainers;
        state.clients = clients;
        state.appointments = appointments;
        render();
        $("#notice").hidden = true;
    } catch (error) {
        showNotice(`Daten konnten nicht geladen werden: ${error.message}`, "error");
    }
}

function render() {
    renderDashboard();
    renderTrainers();
    renderClients();
    renderAppointments();
}

function renderDashboard() {
    const now = new Date();
    const upcoming = state.appointments
        .filter((appointment) => appointment.status === "Scheduled" && new Date(appointment.appointmentDate) >= now)
        .sort((a, b) => new Date(a.appointmentDate) - new Date(b.appointmentDate));

    $("#stat-clients").textContent = state.clients.length;
    $("#stat-trainers").textContent = state.trainers.length;
    $("#stat-upcoming").textContent = upcoming.length;
    $("#upcoming-table").innerHTML = upcoming.length
        ? upcoming.slice(0, 5).map((appointment) => `
            <tr>
                <td>${escapeHtml(formatDate(appointment.appointmentDate))}</td>
                <td>${person(appointment.clientName)}</td>
                <td>${person(appointment.trainerName)}</td>
                <td>${escapeHtml(appointment.durationMinutes)} dk</td>
                <td>${statusBadge(appointment.status)}</td>
            </tr>`).join("")
        : emptyRow(5, "Keine bevorstehenden Termine.");
}

function renderTrainers() {
    const query = $("#trainer-search").value.trim().toLocaleLowerCase("de-DE");
    const trainers = state.trainers.filter((trainer) =>
        [trainer.name, trainer.specialization, trainer.email].some((value) =>
            String(value).toLocaleLowerCase("de-DE").includes(query)));

    $("#trainers-table").innerHTML = trainers.length
        ? trainers.map((trainer) => `
            <tr>
                <td>${person(trainer.name)}</td>
                <td>${escapeHtml(trainer.specialization)}</td>
                <td>${escapeHtml(trainer.email)}</td>
                <td><div class="row-actions">
                    <button class="row-action" data-edit="trainer" data-id="${trainer.id}">Bearbeiten</button>
                    <button class="row-action delete" data-delete="trainer" data-id="${trainer.id}">Löschen</button>
                </div></td>
            </tr>`).join("")
        : emptyRow(4, query ? "Keine passenden Trainer gefunden." : "Noch keine Trainer vorhanden.");
}

function renderClients() {
    const query = $("#client-search").value.trim().toLocaleLowerCase("de-DE");
    const clients = state.clients.filter((client) =>
        [client.name, client.email, client.phone, client.trainerName].some((value) =>
            String(value).toLocaleLowerCase("de-DE").includes(query)));

    $("#clients-table").innerHTML = clients.length
        ? clients.map((client) => `
            <tr>
                <td>${person(client.name)}</td>
                <td>${escapeHtml(client.email)}</td>
                <td>${escapeHtml(client.phone || "—")}</td>
                <td>${escapeHtml(client.trainerName)}</td>
                <td><div class="row-actions">
                    <button class="row-action" data-edit="client" data-id="${client.id}">Bearbeiten</button>
                    <button class="row-action delete" data-delete="client" data-id="${client.id}">Löschen</button>
                </div></td>
            </tr>`).join("")
        : emptyRow(5, query ? "Keine passenden Kunden gefunden." : "Noch keine Kunden vorhanden.");
}

function renderAppointments() {
    const query = $("#appointment-search").value.trim().toLocaleLowerCase("de-DE");
    const status = $("#appointment-status-filter").value;
    const appointments = state.appointments.filter((appointment) =>
        (!status || appointment.status === status)
        && [appointment.clientName, appointment.trainerName].some((value) =>
            String(value).toLocaleLowerCase("de-DE").includes(query)));

    $("#appointments-table").innerHTML = appointments.length
        ? appointments.map((appointment) => `
            <tr>
                <td>${escapeHtml(formatDate(appointment.appointmentDate))}</td>
                <td>${person(appointment.clientName)}</td>
                <td>${person(appointment.trainerName)}</td>
                <td>${escapeHtml(appointment.durationMinutes)} dk</td>
                <td>${statusBadge(appointment.status)}</td>
                <td><div class="row-actions">
                    <button class="row-action" data-edit="appointment" data-id="${appointment.id}">Bearbeiten</button>
                    <button class="row-action delete" data-delete="appointment" data-id="${appointment.id}">Löschen</button>
                </div></td>
            </tr>`).join("")
        : emptyRow(6, "Keine Termine für diese Filter gefunden.");
}

function navigate(view) {
    if (!viewInfo[view]) return;
    state.view = view;
    document.querySelectorAll(".page-view").forEach((section) => section.classList.toggle("active", section.id === `view-${view}`));
    document.querySelectorAll(".nav-link").forEach((button) => button.classList.toggle("active", button.dataset.view === view));
    $("#page-title").textContent = viewInfo[view][0];
    $("#page-description").textContent = viewInfo[view][1];
}

function optionsForTrainers(selectedId) {
    return state.trainers.map((trainer) =>
        `<option value="${trainer.id}" ${trainer.id === selectedId ? "selected" : ""}>${escapeHtml(trainer.name)}</option>`
    ).join("");
}

function optionsForClients(trainerId, selectedId) {
    const clients = state.clients.filter((client) => !trainerId || client.trainerId === Number(trainerId));
    if (!clients.length) return '<option value="" disabled selected>Diesem Trainer sind keine Kunden zugeordnet</option>';
    return clients.map((client) =>
        `<option value="${client.id}" ${client.id === selectedId ? "selected" : ""}>${escapeHtml(client.name)}</option>`
    ).join("");
}

function localDateTimeValue(value) {
    const date = value ? new Date(value) : new Date(Date.now() + 60 * 60 * 1000);
    if (Number.isNaN(date.getTime())) return "";
    const localDate = new Date(date.getTime() - date.getTimezoneOffset() * 60000);
    return localDate.toISOString().slice(0, 16);
}

function field(label, name, value, type = "text", required = true, extra = "") {
    return `<div class="field"><label for="field-${name}">${label}</label><input id="field-${name}" name="${name}" type="${type}" value="${escapeHtml(value)}" ${required ? "required" : ""} ${extra}></div>`;
}

function openForm(resource, id = null) {
    const lists = {
        trainer: state.trainers,
        client: state.clients,
        appointment: state.appointments
    };
    const item = id === null ? null : lists[resource].find((record) => record.id === Number(id));
    if (id !== null && !item) return;
    state.editing = { resource, id: item?.id ?? null };
    $("#form-error").hidden = true;
    $("#form-error").textContent = "";
    $("#dialog-eyebrow").textContent = item ? "EINTRAG BEARBEITEN" : "NEUER EINTRAG";
    $("#dialog-title").textContent = dialogTitles[resource][item ? "edit" : "create"];

    if (resource === "trainer") {
        $("#form-fields").innerHTML = [
            field("Vor- und Nachname", "name", item?.name || ""),
            field("Spezialgebiet", "specialization", item?.specialization || ""),
            field("E-Mail-Adresse", "email", item?.email || "", "email")
        ].join("");
    } else if (resource === "client") {
        if (!state.trainers.length) {
            showNotice("Bitte legen Sie zuerst einen Trainer an, bevor Sie einen Kunden hinzufügen.", "error");
            return;
        }
        $("#form-fields").innerHTML = [
            field("Vor- und Nachname", "name", item?.name || ""),
            field("E-Mail-Adresse", "email", item?.email || "", "email"),
            field("Telefonnummer", "phone", item?.phone || "", "tel", false, 'maxlength="32"'),
            `<div class="field"><label for="field-trainerId">Trainer</label><select id="field-trainerId" name="trainerId" required>${optionsForTrainers(item?.trainerId ?? state.trainers[0].id)}</select></div>`
        ].join("");
    } else {
        if (!state.trainers.length || !state.clients.length) {
            showNotice("Bitte legen Sie zuerst einen Trainer und einen Kunden an, bevor Sie einen Termin hinzufügen.", "error");
            return;
        }
        const trainerId = item?.trainerId
            ?? state.trainers.find((trainer) => state.clients.some((client) => client.trainerId === trainer.id))?.id
            ?? state.trainers[0].id;
        $("#form-fields").innerHTML = `
            <div class="field"><label for="field-trainerId">Trainer</label><select id="field-trainerId" name="trainerId" required>${optionsForTrainers(trainerId)}</select></div>
            <div class="field"><label for="field-clientId">Kunde</label><select id="field-clientId" name="clientId" required>${optionsForClients(trainerId, item?.clientId)}</select></div>
            ${field("Datum und Uhrzeit", "appointmentDate", localDateTimeValue(item?.appointmentDate), "datetime-local")}
            ${field("Dauer (Minuten)", "durationMinutes", item?.durationMinutes ?? 60, "number", true, 'min="15" max="240" step="1"')}
            <div class="field"><label for="field-status">Status</label><select id="field-status" name="status" required>
                <option value="Scheduled" ${!item || item.status === "Scheduled" ? "selected" : ""}>Geplant</option>
                <option value="Completed" ${item?.status === "Completed" ? "selected" : ""}>Abgeschlossen</option>
                <option value="Cancelled" ${item?.status === "Cancelled" ? "selected" : ""}>Abgesagt</option>
            </select></div>`;
    }

    $("#record-dialog").showModal();
}

function updateClientOptions() {
    const trainerSelect = $("#field-trainerId");
    const clientSelect = $("#field-clientId");
    if (!trainerSelect || !clientSelect) return;
    const previousClientId = Number(clientSelect.value);
    clientSelect.innerHTML = optionsForClients(trainerSelect.value, previousClientId);
    if (!clientSelect.value) clientSelect.selectedIndex = 0;
}

function formPayload(resource, formData) {
    if (resource === "trainer") {
        return {
            name: formData.get("name").trim(),
            specialization: formData.get("specialization").trim(),
            email: formData.get("email").trim()
        };
    }
    if (resource === "client") {
        return {
            name: formData.get("name").trim(),
            email: formData.get("email").trim(),
            phone: formData.get("phone").trim() || null,
            trainerId: Number(formData.get("trainerId"))
        };
    }
    return {
        appointmentDate: new Date(formData.get("appointmentDate")).toISOString(),
        durationMinutes: Number(formData.get("durationMinutes")),
        status: formData.get("status"),
        trainerId: Number(formData.get("trainerId")),
        clientId: Number(formData.get("clientId"))
    };
}

async function saveRecord(event) {
    event.preventDefault();
    if (!state.editing) return;
    const { resource, id } = state.editing;
    const errorBox = $("#form-error");
    const saveButton = $("#save-button");
    saveButton.disabled = true;
    errorBox.hidden = true;

    try {
        const payload = formPayload(resource, new FormData(event.currentTarget));
        const path = `/api/${resource === "trainer" ? "trainers" : resource === "client" ? "clients" : "appointments"}${id === null ? "" : `/${id}`}`;
        await api(path, {
            method: id === null ? "POST" : "PUT",
            body: JSON.stringify(payload)
        });
        $("#record-dialog").close();
        showNotice(id === null ? "Eintrag erfolgreich erstellt." : "Änderungen gespeichert.");
        await loadData();
    } catch (error) {
        errorBox.textContent = error.message;
        errorBox.hidden = false;
    } finally {
        saveButton.disabled = false;
    }
}

async function deleteRecord(resource, id) {
    const names = { trainer: "diesen Trainer", client: "diesen Kunden", appointment: "diesen Termin" };
    if (!window.confirm(`Möchten Sie ${names[resource]} wirklich löschen?`)) return;
    const endpoint = resource === "trainer" ? "trainers" : resource === "client" ? "clients" : "appointments";
    try {
        await api(`/api/${endpoint}/${id}`, { method: "DELETE" });
        showNotice("Eintrag gelöscht.");
        await loadData();
    } catch (error) {
        showNotice(`Eintrag konnte nicht gelöscht werden: ${error.message}`, "error");
    }
}

document.querySelectorAll(".nav-link").forEach((button) => button.addEventListener("click", () => navigate(button.dataset.view)));
document.querySelectorAll("[data-navigate]").forEach((button) => button.addEventListener("click", () => navigate(button.dataset.navigate)));
document.querySelectorAll("[data-create]").forEach((button) => button.addEventListener("click", () => openForm(button.dataset.create)));
document.querySelectorAll("[data-close-dialog]").forEach((button) => button.addEventListener("click", () => $("#record-dialog").close()));
$("#record-form").addEventListener("submit", saveRecord);
$("#form-fields").addEventListener("change", (event) => {
    if (event.target.id === "field-trainerId") updateClientOptions();
});
$("#trainer-search").addEventListener("input", renderTrainers);
$("#client-search").addEventListener("input", renderClients);
$("#appointment-search").addEventListener("input", renderAppointments);
$("#appointment-status-filter").addEventListener("change", renderAppointments);
document.addEventListener("click", (event) => {
    const editButton = event.target.closest("[data-edit]");
    if (editButton) {
        navigate(editButton.dataset.edit === "trainer" ? "trainers" : editButton.dataset.edit === "client" ? "clients" : "appointments");
        openForm(editButton.dataset.edit, editButton.dataset.id);
        return;
    }
    const deleteButton = event.target.closest("[data-delete]");
    if (deleteButton) deleteRecord(deleteButton.dataset.delete, Number(deleteButton.dataset.id));
});

loadData();
