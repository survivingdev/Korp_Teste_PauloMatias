import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'produtos',
  },
  {
    path: 'produtos',
    loadComponent: () =>
      import('./paginas/produtos/produtos').then(
        (componente) => componente.Produtos,
      ),
  },
  {
    path: 'notas-fiscais',
    loadComponent: () =>
      import('./paginas/notas-fiscais/notas-fiscais').then(
        (componente) => componente.NotasFiscais,
      ),
  },
  {
    path: '**',
    redirectTo: 'produtos',
  },
];