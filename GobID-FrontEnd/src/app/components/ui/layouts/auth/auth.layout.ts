import { Component } from '@angular/core';
import { Footer } from '../../footer/footer';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-auth.layout',
  imports: [Footer, RouterOutlet],
  templateUrl: './auth.layout.html',
})
export class AuthLayout {}
