// Marca o body para que o CSS @media print mostre apenas o conteúdo do modal de impressão.
window.printModalContent = function () {
    document.body.classList.add('pedido-print-modal-active');
    var cleanup = function () {
        document.body.classList.remove('pedido-print-modal-active');
        window.removeEventListener('afterprint', cleanup);
    };
    window.addEventListener('afterprint', cleanup);
    window.print();
};
