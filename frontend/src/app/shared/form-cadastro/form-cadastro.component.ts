import { Component, Inject, OnInit, OnDestroy } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { Subject, takeUntil } from 'rxjs';
import { Cachorro, CreateCachorroDto } from 'src/app/core/models/cachorro.model';
import { CreateHorarioDto } from 'src/app/core/models/horario.model';
import { CreatePetShopDto } from 'src/app/core/models/petshop.model';
import { CachorroService } from 'src/app/core/services/cachorro.service';
import { HorarioService } from 'src/app/core/services/horario.service';
import { ValidaRacaService } from 'src/app/core/services/valida-raca.service';
import { PetShopService } from 'src/app/core/services/petshop.service';

@Component({
  selector: 'app-form-cadastro',
  templateUrl: './form-cadastro.component.html',
  styleUrls: ['./form-cadastro.component.scss']
})
export class FormCadastroComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  tipo: 'cachorro' | 'horario' | 'petshop';
  
  novoCachorro: any = {
    nomeCachorro: '',
    nomeTutor: '',
    contatoTutor: '',
    raca: '',
    porte: ''
  };

  novoPetShop: any = {
    email: '',
    senha: '',
    confirmarSenha: '',
    enderecoPetShop: ''
  };

  racasDisponiveis: string[] = [];
  cachorroSelecionadoObj?: Cachorro;

  novoHorario: any = {
    cachorros: [],
    data: '',
    horario: '',
    valorTotal: 0,
    servicosBaseSelecionado: '',
    adicionais: []
  };
  listaCachorros: Cachorro[] = [];
  servicosBaseDisponiveis: string[] = [];
  servicosAdicionaisDisponiveis: string[] = [];
  calculandoValor = false;
  erroValor = false;
  private calculoAtual = 0;

  cachorroSelecionado: string = '';
  cachorrosFiltrados: Cachorro[] = [];

  constructor(
    @Inject(MAT_DIALOG_DATA) public data: any,
    private dialogRef: MatDialogRef<FormCadastroComponent>,
    private cachorroService: CachorroService,
    private horarioService: HorarioService,
    private validaRacaService: ValidaRacaService,
    private petShopService: PetShopService
  ) {
    this.tipo = data.tipo;
  }

  ngOnInit() {
    if (this.tipo === 'horario') {
      this.cachorroService.cachorros$
        .pipe(takeUntil(this.destroy$))
        .subscribe(cachorros => {
          this.listaCachorros = cachorros;
          this.cachorrosFiltrados = cachorros;
        });

      this.carregarServicosAdicionais();
    }
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  carregarServicosAdicionais() {
    this.validaRacaService.getServicosAdicionais().subscribe({
      next: (servicos) => {
        this.servicosAdicionaisDisponiveis = servicos;
      },
      error: (err) => {
        console.error('Erro ao carregar serviços adicionais', err);
        this.servicosAdicionaisDisponiveis = [];
      }
    });
  }

  selecionarCachorro(nome: string) {
    const selecionado = this.listaCachorros.find(c => c.nomeCachorro === nome);

    if (selecionado) {
      ++this.calculoAtual;
      this.calculandoValor = false;
      this.erroValor = false;
      this.cachorroSelecionado = selecionado.nomeCachorro;
      this.cachorroSelecionadoObj = selecionado;
      this.novoHorario.valorTotal = 0;
      this.novoHorario.adicionais = [];

      this.carregarServicosBase(selecionado.raca, selecionado.porte);
    }
  }

  carregarServicosBase(raca: string, porte: string) {
    this.validaRacaService.getServicosPorRacaPorte(raca, porte).subscribe({
      next: (servicos) => {
        this.servicosBaseDisponiveis = servicos;
        this.novoHorario.servicosBaseSelecionado = '';
        this.novoHorario.valorTotal = 0;
      },
      error: (err) => {
        console.error('Erro ao carregar serviços base', err);
        this.servicosBaseDisponiveis = [];
      }
    });
  }

  filtrarCachorros() {
    const termo = this.cachorroSelecionado.toLowerCase();
    this.cachorrosFiltrados = this.listaCachorros.filter(c =>
      c.nomeCachorro.toLowerCase().includes(termo) ||
      c.nomeTutor.toLowerCase().includes(termo)
    );
  }

  atualizarRacas() {
    if (this.tipo === 'cachorro') {
      console.log('Porte selecionado:', this.novoCachorro.porte);

      this.validaRacaService.getRacasPorPorte(this.novoCachorro.porte).subscribe({
        next: (racas: string[]) => {
          console.log('Raças recebidas da API:', racas);
          this.racasDisponiveis = racas;
        },
        error: (err: any) => {
          console.error('Erro ao carregar raças', err);
          this.racasDisponiveis = [];
        }
      });
    }
  }

  calcularValor() {
    const calculo = ++this.calculoAtual;
    this.erroValor = false;
    if (!this.cachorroSelecionadoObj || !this.novoHorario.servicosBaseSelecionado) {
      this.novoHorario.valorTotal = 0;
      this.calculandoValor = false;
      return;
    }

    this.calculandoValor = true;
    this.novoHorario.valorTotal = 0;
    this.horarioService.calcularPreco(
      this.cachorroSelecionadoObj.raca,
      this.cachorroSelecionadoObj.porte,
      this.novoHorario.servicosBaseSelecionado,
      this.novoHorario.adicionais || []
    ).subscribe({
      next: (valor) => {
        if (calculo !== this.calculoAtual) return;
        this.novoHorario.valorTotal = valor;
        this.calculandoValor = false;
      },
      error: (err) => {
        if (calculo !== this.calculoAtual) return;
        this.novoHorario.valorTotal = 0;
        this.calculandoValor = false;
        this.erroValor = true;
        console.error('Erro ao calcular preço', err);
      }
    });
  }

  salvar() {
    if (this.tipo === 'cachorro') {
      const dto: CreateCachorroDto = {
        nomeCachorro: this.novoCachorro.nomeCachorro,
        nomeTutor: this.novoCachorro.nomeTutor,
        contatoTutor: this.novoCachorro.contatoTutor,
        raca: this.novoCachorro.raca,
        porte: this.novoCachorro.porte
      };

      this.cachorroService.salvarCachorro(dto).subscribe({
        next: () => {
          alert('Cachorro cadastrado com sucesso!');
          this.dialogRef.close(true);
        },
        error: (err: any) => alert(err.message)
      });
    }
    else if (this.tipo === 'horario') {
      const dto: CreateHorarioDto = {
        cachorroId: this.cachorroSelecionadoObj!.id,
        data: `${this.novoHorario.data} ${this.novoHorario.horario}`,
        servicoBaseSelecionado: this.novoHorario.servicosBaseSelecionado,
        adicionais: this.novoHorario.adicionais
      };

      this.horarioService.salvarHorario(dto).subscribe({
        next: () => {
          alert('Horário cadastrado com sucesso!');
          this.dialogRef.close(true);
        },
        error: (err: any) => alert(err.message)
      });
    }
    else if (this.tipo === 'petshop') {
      const dto: CreatePetShopDto = {
        email: this.novoPetShop.email,           // ← AGORA USA novoPetShop
        senha: this.novoPetShop.senha,
        confirmarSenha: this.novoPetShop.confirmarSenha,
        enderecoPetShop: this.novoPetShop.enderecoPetShop  // ← AGORA USA novoPetShop
      };

      this.petShopService.criarPetShop(dto).subscribe({
        next: () => {
          alert('Pet Shop cadastrado com sucesso! Faça o login.');
          this.dialogRef.close(true);
        },
        error: (err: any) => alert(err.message)
      });
    }
  }
}
