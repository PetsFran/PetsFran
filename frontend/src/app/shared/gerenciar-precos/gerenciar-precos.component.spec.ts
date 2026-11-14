import { of } from 'rxjs';
import { MatDialogRef } from '@angular/material/dialog';
import { TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { GerenciarPrecosComponent } from './gerenciar-precos.component';
import { PrecoServico, PrecosService } from '../../core/services/precos.service';

describe('GerenciarPrecosComponent', () => {
  const preco: PrecoServico = {
    tipo: 'Base', porte: 'Pequeno', raca: 'Vira lata', nome: 'Banho',
    valor: 60
  };
  let precos: jasmine.SpyObj<PrecosService>;
  let componente: GerenciarPrecosComponent;

  beforeEach(() => {
    precos = jasmine.createSpyObj<PrecosService>('PrecosService', ['listar', 'salvar']);
    precos.listar.and.returnValue(of([preco]));
    componente = new GerenciarPrecosComponent(precos, {} as MatDialogRef<GerenciarPrecosComponent>);
    componente.ngOnInit();
  });

  it('aceita vírgula decimal e reflete o preço salvo', () => {
    precos.salvar.and.returnValue(of({ ...preco, valor: 87.5 }));
    const linha = componente.linhas[0];
    linha.edicao = '87,50';

    componente.salvar(linha);

    expect(precos.salvar).toHaveBeenCalledWith(linha, 87.5);
    expect(linha.valor).toBe(87.5);
    expect(linha.edicao).toBe('87,50');
  });

  it('impede valores negativos e com mais de duas casas decimais', () => {
    const linha = componente.linhas[0];
    linha.edicao = '-1';
    componente.salvar(linha);
    linha.edicao = '12,345';
    componente.salvar(linha);

    expect(precos.salvar).not.toHaveBeenCalled();
    expect(linha.erro).toContain('duas casas decimais');
  });

  it('mostra apenas o preço atual e alterna entre serviços base e adicionais', async () => {
    precos.listar.and.returnValue(of([preco, {
      tipo: 'Adicional', porte: 'P', raca: '', nome: 'Cortar Unha',
      valor: 10
    }]));
    TestBed.configureTestingModule({
      declarations: [GerenciarPrecosComponent],
      imports: [FormsModule],
      providers: [
        { provide: PrecosService, useValue: precos },
        { provide: MatDialogRef, useValue: { close: jasmine.createSpy('close') } }
      ]
    });
    const fixture = TestBed.createComponent(GerenciarPrecosComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    const raiz = fixture.nativeElement as HTMLElement;
    expect(raiz.querySelector('.linha')?.textContent).toContain('Banho');
    expect((raiz.querySelector('.linha input[name=valor]') as HTMLInputElement).value).toBe('60,00');
    expect(raiz.textContent).not.toContain('Preço padrão');
    expect(raiz.textContent).not.toContain('Personalizado');
    expect(raiz.textContent).not.toContain('Restaurar');

    (raiz.querySelectorAll('.abas button')[1] as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(raiz.querySelector('.linha')?.textContent).toContain('Cortar Unha');
    expect(raiz.querySelector('.linha')?.textContent).toContain('Pequeno');
  });
});
