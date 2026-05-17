// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
document.querySelectorAll('[data-theme-toggle]').forEach((botao) => {
    botao.addEventListener('click', () => {
        const temaAtual = document.documentElement.dataset.theme === 'dark' ? 'dark' : 'light';
        const proximoTema = temaAtual === 'dark' ? 'light' : 'dark';

        document.documentElement.dataset.theme = proximoTema;
        localStorage.setItem('evolution-theme', proximoTema);
    });
});

document.querySelectorAll('[data-form-envio-mensagem]').forEach((form) => {
    form.addEventListener('submit', () => {
        const botao = form.querySelector('[data-botao-envio]');
        const spinner = form.querySelector('[data-spinner-envio]');
        const texto = form.querySelector('[data-texto-envio]');

        if (!botao) {
            return;
        }

        botao.disabled = true;
        botao.setAttribute('aria-busy', 'true');

        if (spinner) {
            spinner.classList.remove('d-none');
        }

        if (texto) {
            texto.textContent = botao.dataset.textoEnviando || 'Enviando...';
        }
    });
});
