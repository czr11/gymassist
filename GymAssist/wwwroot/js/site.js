// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

document.getElementById('payment-client')?.addEventListener('change', function () {
	const price = this.options[this.selectedIndex]?.dataset.monthlyPrice;
	if (price) document.getElementById('payment-amount').value = price;
});

document.getElementById('resultModalClose')?.addEventListener('click', function () {
	document.getElementById('resultModal')?.remove();
});

let pendingLogoutForm;
const logoutModal = document.getElementById('logoutConfirmModal');
const confirmLogoutButton = document.getElementById('confirmLogoutButton');

document.querySelectorAll('.logout-form').forEach(function (form) {
	form.addEventListener('submit', function (event) {
		event.preventDefault();
		pendingLogoutForm = form;
		bootstrap.Modal.getOrCreateInstance(logoutModal).show();
	});
});

confirmLogoutButton?.addEventListener('click', function () {
	if (!pendingLogoutForm) return;
	HTMLFormElement.prototype.submit.call(pendingLogoutForm);
});
