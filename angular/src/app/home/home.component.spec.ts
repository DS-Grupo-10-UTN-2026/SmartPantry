import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Component } from '@angular/core';
import { HomeComponent } from './home.component';
import { AuthService, LocalizationPipe } from '@abp/ng.core';

@Component({
  selector: 'app-home',
  template: '',
  imports: [LocalizationPipe],
})
class TestHostComponent {}

describe('HomeComponent', () => {
  let component: HomeComponent;
  let fixture: ComponentFixture<HomeComponent>;

  const authServiceMock = {
    isAuthenticated: false,
    navigateToLogin: () => {},
  };

  beforeEach(async () => {
    TestBed.overrideComponent(HomeComponent, {
      set: {
        imports: [],
        template: '',
      },
    });

    await TestBed.configureTestingModule({
      imports: [HomeComponent],
    })
      .overrideProvider(AuthService, { useValue: authServiceMock })
      .compileComponents();

    fixture = TestBed.createComponent(HomeComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should reflect hasLoggedIn from AuthService', () => {
    expect(component.hasLoggedIn).toBe(false);
  });
});