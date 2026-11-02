//Global function. Forat saved UTC times for local browser datetime when displaying
function formatLocalDateTimes() {
	document.querySelectorAll("[data-utc-datetime]").forEach(element => {
		const utcValue = element.dataset.utcDatetime;

		if (!utcValue) return;

		// Ensure values without timezone information are interpreted as UTC.
		const normalizedValue = /(?:Z|[+-]\d{2}:\d{2})$/i.test(utcValue)
			? utcValue
			: `${utcValue}Z`;

		const date = new Date(normalizedValue);

		if (Number.isNaN(date.getTime())) return;

		element.textContent = date.toLocaleString(undefined, {
			year: "numeric",
			month: "short",
			day: "numeric",
			hour: "numeric",
			minute: "2-digit"
		});
	});
}

document.addEventListener("DOMContentLoaded", formatLocalDateTimes);