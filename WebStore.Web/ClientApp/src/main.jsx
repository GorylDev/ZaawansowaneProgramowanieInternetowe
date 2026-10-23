import React from 'react';
import { createRoot } from 'react-dom/client';
import './style.css';

function App() {
  return (
    <main>
      <span className="label">PROJEKT ZPI · SPRINT 1</span>
      <h1>WebStore</h1>
      <p className="intro">Podstawa aplikacji sklepu jest gotowa do dalszego rozwoju.</p>
      <section aria-labelledby="scope">
        <h2 id="scope">Zakres pierwszej iteracji</h2>
        <p>Model produktów, klientów, zamówień, faktur, adresów i sklepów stacjonarnych oraz usługi do pracy z tymi danymi.</p>
        <p>W kolejnych iteracjach powstaną ekrany obsługi sklepu. Ten widok stanowi szkielet klienta.</p>
      </section>
      <footer>Zaawansowane Programowanie Internetowe</footer>
    </main>
  );
}

createRoot(document.getElementById('root')).render(<App />);
