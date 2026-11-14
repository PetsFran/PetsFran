import { of } from 'rxjs';
import { MatDialogRef } from '@angular/material/dialog';
import { TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { GerenciarPrecosComponent } from './gerenciar-precos.component';
import { PrecoServico, PrecosService } from '../../core/services/precos.service';

describe('GerenciarPrecosComponent', () => {
  const padrao: PrecoServico = {
    tipo: 'Base', porte: 'Pequeno', raca: 'Vira lata', nome: 'Banho',
    valorPadrao: 60, valor: 60, personalizado: false
  };
  let precos: jasmine.SpyObj<PrecosService>;
  let componente: GerenciarPrecosComponent;

  beforeEach(() => {
    precos = jasmine.createSpyObj<PrecosService>('PrecosService', ['listar', 'salvar', 'restaurar']);
    precos.listar.and.returnValue(of([padrao]));
    componente = new GerenciarPrecosComponent(precos, {} as MatDialogRef<GerenciarPrecosComponent>);
    componente.ngOnInit();
  });

  it('aceita vírgula decimal e reflete o preço salvo', () => {
    precos.salvar.and.returnValue(of({ ...padrao, valor: 87.5, personalizado: true }));
    const linha = componente.linhas[0];
    linha.edicao = '87,50';

    componente.salvar(linha);

    expect(precos.salvar).toHaveBeenCalledWith(linha, 87.5);
    expect(linha.valor).toBe(87.5);
    expect(linha.personalizado).toBeTrue();
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

  it('restaura o preço padrão depois de uma personalização', () => {
    const linha = componente.linhas[0];
    linha.valor = 87.5;
    linha.personalizado = true;
    precos.restaurar.and.returnValue(of(padrao));

    componente.restaurar(linha);

    expect(precos.restaurar).toHaveBeenCalledWith(linha);
    expect(linha.valor).toBe(60);
    expect(linha.personalizado).toBeFalse();
    expect(linha.edicao).toBe('60,00');
  });

  it('mostra o catálogo e alterna entre serviços base e adicionais', () => {
    precos.listar.and.returnValue(of([padrao, {
      tipo: 'Adicional', porte: 'P', raca: '', nome: 'Cortar Unha',
      valorPadrao: 10, valor: 10, personalizado: false
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
    const raiz = fixture.nativeElement as HTMLElement;
    expect(raiz.querySelector('.linha')?.textContent).toContain('Banho');

    (raiz.querySelectorAll('.abas button')[1] as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(raiz.querySelector('.linha')?.textContent).toContain('Cortar Unha');
    expect(raiz.querySelector('.linha')?.textContent).toContain('Pequeno');
  });
});
