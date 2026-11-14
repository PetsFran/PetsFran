import { MatDialog } from '@angular/material/dialog';
import { Router } from '@angular/router';
import { AppComponent } from './app.component';
import { AuthService } from './core/services/auth.service';
import { GerenciarPrecosComponent } from './shared/gerenciar-precos/gerenciar-precos.component';

describe('AppComponent', () => {
  it('abre a gestão de preços pelo painel', () => {
    const dialog = jasmine.createSpyObj<MatDialog>('MatDialog', ['open']);
    const auth = jasmine.createSpyObj<AuthService>('AuthService', ['getToken']);
    const router = jasmine.createSpyObj<Router>('Router', ['navigate']);
    const componente = new AppComponent(dialog, auth, router);

    componente.abrirGerenciarPrecos();

    expect(dialog.open).toHaveBeenCalledWith(GerenciarPrecosComponent, jasmine.objectContaining({ width: '900px' }));
  });
});
