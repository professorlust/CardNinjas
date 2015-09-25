﻿using UnityEngine;
using Assets.Scripts.Util;
using Assets.Scripts.Grid;

namespace Assets.Scripts.Player
{
    public class Player : Character
    {
        public delegate void NewSelectedCard(string name, string type, int range, int damage, string description);
        public static event NewSelectedCard NewSelect;

        [SerializeField]
        private Animator anim;
        [SerializeField]
        private GameObject bullet;
        [SerializeField]
        private Transform barrel;

        private static int damage = 0;
        private static bool doOnce = false;
        private static bool move = false;
        private static bool useCard = false;
        private static bool basicAttack = false;
        private static bool takeDamage = false;
        private static bool invun = false;
        private static float invunTime = .5f;
        private static float invunTimer = 0;
        private static float hold = 0;//used for delays
        private static Enums.Direction direction;
        private static GridNode nextNode;

        private float renderTime = .002f;
        private float renderTimer = 0;
        private bool animDone = false;
        private bool hit = false;
        private bool render = false;
        private PlayerStateMachine machine;
        private delegate void state();
        private state[] doState;
        private Enums.PlayerState prevState = 0;
        private Enums.PlayerState currState = 0;
        private CardSystem.Card[] cards;
        private int currentCard;

        void Start()
        {
            grid = FindObjectOfType<GridManager>().Grid;
            currentNode = grid[rowStart, colStart];
            currentNode.Owner = this;
            transform.position = currentNode.transform.position;
            cards = FindObjectOfType<CardSystem.CardList>().Cards;
            currentCard = 0;
            CardUIEvent(); //fire event to update card UI
            //state machine init
            machine = new PlayerStateMachine();
            doState = new state[] { Idle, MoveBegining, MoveEnding, Hit, Dead, BasicAttack, Sword };
            renderTimer = 0;
            invunTimer = 0;
        }

        void Update()
        {
            if (CustomInput.BoolFreshPress(CustomInput.UserInput.Up))
            {
                if (currentNode.panelAllowed(Enums.Direction.Up, Type))
                {
                    direction = Enums.Direction.Up;
                    nextNode = currentNode.Up;
                }
            }
            else if (CustomInput.BoolFreshPress(CustomInput.UserInput.Down))
            {
                if (currentNode.panelAllowed(Enums.Direction.Down, Type))
                {
                    direction = Enums.Direction.Down;
                    nextNode = currentNode.Down;
                }
            }
            else if (CustomInput.BoolFreshPress(CustomInput.UserInput.Left))
            {
                if (currentNode.panelAllowed(Enums.Direction.Left, Type))
                {
                    direction = Enums.Direction.Left;
                    nextNode = currentNode.Left;
                }
            }
            else if (CustomInput.BoolFreshPress(CustomInput.UserInput.Right))
            {
                if (currentNode.panelAllowed(Enums.Direction.Right, Type))
                {
                    direction = Enums.Direction.Right;
                    nextNode = currentNode.Right;
                }
            }
            else
                direction = Enums.Direction.None;
            //get next state
            currState = machine.update(hit, animDone, direction, cards[currentCard].Type);
            //state clean up
            if (prevState != currState)
            {
                doOnce = false;
                animDone = false;
                hit = false;
                anim.SetInteger("state", (int)currState);
            }
            if (invunTimer > 0)
            {
                if (renderTimer > renderTime)
                {
                    render = !render;
                    renderTimer = 0;
                    //GetComponent<Renderer>().enabled = render;
                }
                hit = false;
                renderTimer += Time.deltaTime;
                invunTimer -= Time.deltaTime;
            }
            else
            {
                //GetComponent<Renderer>().enabled = true;
                invun = false;
            }

            //run state
            doState[(int)currState]();

            if (move)
            {
                move = false;
                currentNode.clearOccupied();
                currentNode = nextNode;
                currentNode.Owner = (this);
                transform.position = currentNode.transform.position;
            }

            if (useCard)
            {
                useCard = false;
                cards[currentCard++].Action.useCard(this);
                if (currentCard >= cards.Length)
                    currentCard = 0;
                CardUIEvent();
            }

            if (basicAttack)
            {
                basicAttack = false;
                Weapons.Projectiles.Bullet b = Instantiate(bullet).GetComponent<Weapons.Projectiles.Bullet>();
                b.transform.position = barrel.position;
                b.Direction = Direction;
            }

            if (damage > 0 && takeDamage)
            {
                takeDamage = false;
                TakeDamage(damage);
                damage = 0;
            }
            prevState = currState;
        }

        public void AnimDetector()
        {
            animDone = true;
        }

        private void CardUIEvent()
        {
            if (NewSelect != null)
                NewSelect(cards[currentCard].Name, cards[currentCard].Type.ToString(),
                              cards[currentCard].Action.Range, cards[currentCard].Action.Damage, cards[currentCard].Description); //fire event to gui
        }

        void OnTriggerEnter(Collider col)
        {
            Weapons.Hitbox hitbox = col.gameObject.GetComponent<Weapons.Hitbox>();
            if (hitbox != null && !invun)
            {
                hit = true;
                damage = hitbox.Damage;
            }
        }

        private static void Idle()
        {
        }

        private static void MoveBegining()
        {
        }

        private static void MoveEnding()
        {
            if (!doOnce)
            {
                doOnce = true;
                move = true;
            }
        }

        private static void Hit()
        {
            if (!doOnce)
            {
                doOnce = true;
                invunTimer = invunTime;
                invun = true;
                takeDamage = true;
            }
        }

        private static void Dead()
        {
        }

        private static void BasicAttack()
        {
            hold += Time.deltaTime;
            if (hold > .5f)
            {
                hold = 0;
                basicAttack = true;
            }
        }

        private static void Sword()
        {
            if (!doOnce)
            {
                doOnce = true;
                useCard = true;
            }
        }
    }
}