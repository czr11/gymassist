// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

document.getElementById('payment-client')?.addEventListener('change', function () {
	const price = this.options[this.selectedIndex]?.dataset.monthlyPrice;
	if (price) document.getElementById('payment-amount').value = price;
});

document.getElementById('resultModalClose')?.addEventListener('click', function () {
	document.getElementById('resultModal')?.remove();
});

let pendingActionForm;
const actionModal = document.getElementById('actionConfirmModal');
const actionTitle = document.getElementById('actionConfirmTitle');
const actionMessage = document.getElementById('actionConfirmMessage');
const confirmActionButton = document.getElementById('confirmActionButton');

document.querySelectorAll('.confirm-action-form').forEach(function (form) {
	form.addEventListener('submit', function (event) {
		event.preventDefault();
		pendingActionForm = form;
		actionTitle.textContent = form.dataset.confirmTitle;
		actionMessage.textContent = form.dataset.confirmMessage;
		confirmActionButton.textContent = form.dataset.confirmButton;
		bootstrap.Modal.getOrCreateInstance(actionModal).show();
	});
});

confirmActionButton?.addEventListener('click', function () {
	if (!pendingActionForm) return;
	HTMLFormElement.prototype.submit.call(pendingActionForm);
});
